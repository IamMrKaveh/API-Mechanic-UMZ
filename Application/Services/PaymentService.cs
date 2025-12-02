namespace Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ICartRepository _cartRepository;
    private readonly FrontendUrlsDto _frontendUrls;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IOrderRepository orderRepository,
        IOrderStatusRepository orderStatusRepository,
        IEnumerable<IPaymentGateway> gateways,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IAuditService auditService,
        INotificationService notificationService,
        ICartRepository cartRepository,
        IOptions<FrontendUrlsDto> frontendUrlsOptions,
        ILogger<PaymentService> logger)
    {
        _orderRepository = orderRepository;
        _orderStatusRepository = orderStatusRepository;
        _gateways = gateways;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _auditService = auditService;
        _notificationService = notificationService;
        _cartRepository = cartRepository;
        _frontendUrls = frontendUrlsOptions.Value;
        _logger = logger;
    }

    public async Task<(string? PaymentUrl, string? Authority, string? Error)> InitiatePaymentAsync(int orderId, int userId, decimal amount, string description, string? mobile, string? email, string gatewayName)
    {
        var gateway = _gateways.FirstOrDefault(g => g.GatewayName.Equals(gatewayName, StringComparison.OrdinalIgnoreCase));
        if (gateway == null)
        {
            _logger.LogError("Payment gateway {GatewayName} not found.", gatewayName);
            return (null, null, "درگاه پرداخت انتخابی معتبر نیست.");
        }

        var callbackUrl = $"{_frontendUrls.BaseUrl}/payment/callback?orderId={orderId}";

        var initiationDto = new PaymentInitiationDto
        {
            Amount = amount,
            Description = description,
            CallbackUrl = callbackUrl,
            Mobile = mobile,
            Email = email,
            OrderId = orderId,
            UserId = userId
        };

        try
        {
            var result = await gateway.RequestPaymentAsync(initiationDto);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Authority))
            {
                var paymentTx = new PaymentTransaction
                {
                    OrderId = orderId,
                    Authority = result.Authority,
                    Amount = amount * 10,
                    Gateway = gateway.GatewayName,
                    Status = PaymentTransaction.PaymentStatuses.Pending,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = "system"
                };

                await _orderRepository.AddPaymentTransactionAsync(paymentTx);
                await _unitOfWork.SaveChangesAsync();

                return (result.PaymentUrl, result.Authority, null);
            }
            else
            {
                return (null, null, result.Message ?? "خطا در ایجاد تراکنش بانکی.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment for order {OrderId} via {Gateway}", orderId, gatewayName);
            return (null, null, "خطا در ارتباط با درگاه پرداخت.");
        }
    }

    public async Task<PaymentVerificationResultDto> VerifyPaymentAsync(string authority, string status)
    {
        string frontendUrl = _frontendUrls.BaseUrl;

        if (string.IsNullOrEmpty(authority))
        {
            return new PaymentVerificationResultDto
            {
                IsVerified = false,
                RedirectUrl = $"{frontendUrl}/payment/failure?reason=invalid_authority",
                Message = "شناسه پرداخت نامعتبر است."
            };
        }

        var lockKey = $"payment_verify:{authority}";
        if (!await _cacheService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
        {
            return new PaymentVerificationResultDto
            {
                IsVerified = false,
                RedirectUrl = $"{frontendUrl}/payment/failure?reason=processing",
                Message = "تراکنش در حال پردازش است."
            };
        }

        try
        {
            var transaction = await _orderRepository.GetPaymentTransactionAsync(authority);
            if (transaction == null)
            {
                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?reason=tx_not_found",
                    Message = "تراکنش یافت نشد."
                };
            }

            if (transaction.Status == PaymentTransaction.PaymentStatuses.Success)
            {
                return new PaymentVerificationResultDto
                {
                    IsVerified = true,
                    RedirectUrl = $"{frontendUrl}/payment/success?orderId={transaction.OrderId}&refId={transaction.RefId}",
                    RefId = transaction.RefId,
                    Message = "تراکنش قبلا تایید شده است."
                };
            }

            var order = await _orderRepository.GetOrderWithItemsAsync(transaction.OrderId);
            if (order == null)
            {
                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?reason=order_not_found",
                    Message = "سفارش مرتبط یافت نشد."
                };
            }

            if (!status.Equals("OK", StringComparison.OrdinalIgnoreCase) && !status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Status = PaymentTransaction.PaymentStatuses.Failed;
                transaction.ErrorMessage = "User Canceled or Gateway Rejected";
                await _unitOfWork.SaveChangesAsync();

                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?reason=canceled&orderId={order.Id}",
                    Message = "پرداخت ناموفق بود یا توسط کاربر لغو شد."
                };
            }

            if (transaction.Amount != order.FinalAmount * 10)
            {
                _logger.LogWarning("Amount mismatch. Tx: {TxAmount}, Order: {OrderAmount}", transaction.Amount, order.FinalAmount * 10);

            }

            var gateway = _gateways.FirstOrDefault(g => g.GatewayName.Equals(transaction.Gateway, StringComparison.OrdinalIgnoreCase));
            if (gateway == null)
            {
                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?reason=gateway_error",
                    Message = "درگاه پرداخت یافت نشد."
                };
            }

            var verificationResult = await gateway.VerifyPaymentAsync(transaction.Amount, authority);

            if (verificationResult.IsVerified)
            {
                return await CompletePaymentSuccessAsync(order, transaction, verificationResult, frontendUrl);
            }
            else
            {
                transaction.Status = PaymentTransaction.PaymentStatuses.Failed;
                transaction.ErrorMessage = verificationResult.Message ?? "Verification Failed";
                transaction.VerificationAttemptedAt = DateTime.UtcNow;
                transaction.VerificationCount++;

                await _unitOfWork.SaveChangesAsync();

                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?reason=verification_failed&orderId={order.Id}",
                    Message = verificationResult.Message ?? "تایید تراکنش در سمت بانک ناموفق بود."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during payment verification for authority {Authority}", authority);
            return new PaymentVerificationResultDto
            {
                IsVerified = false,
                RedirectUrl = $"{frontendUrl}/payment/failure?reason=exception",
                Message = "خطای سیستمی در تایید پرداخت."
            };
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(lockKey);
        }
    }

    private async Task<PaymentVerificationResultDto> CompletePaymentSuccessAsync(
        Domain.Order.Order order,
        PaymentTransaction transaction,
        GatewayVerificationResultDto verificationResponse,
        string frontendUrl)
    {
        using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var paidStatus = await _orderStatusRepository.GetStatusByNameAsync("Paid")
                             ?? await _orderStatusRepository.GetStatusByNameAsync("Processing");

            if (paidStatus != null)
            {
                order.OrderStatusId = paidStatus.Id;
            }

            order.IsPaid = true;

            transaction.Status = PaymentTransaction.PaymentStatuses.Success;
            transaction.RefId = verificationResponse.RefId;
            transaction.CardPan = verificationResponse.CardPan;
            transaction.CardHash = verificationResponse.CardHash;
            transaction.Fee = verificationResponse.Fee;
            transaction.VerifiedAt = DateTime.UtcNow;
            transaction.VerificationCount++;

            if (order.DiscountUsages != null)
            {
                foreach (var usage in order.DiscountUsages)
                {
                    usage.IsConfirmed = true;
                    if (usage.DiscountCode != null)
                    {
                        usage.DiscountCode.UsedCount++;
                    }
                }
            }

            var cart = await _cartRepository.GetCartAsync(order.UserId);
            if (cart != null && cart.CartItems.Any())
            {
                _cartRepository.RemoveCartItems(cart.CartItems);
                await _cacheService.ClearAsync($"cart:user:{order.UserId}");
            }

            await _unitOfWork.SaveChangesAsync();
            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "پرداخت موفق",
                $"سفارش #{order.Id} با موفقیت پرداخت شد. کد پیگیری: {verificationResponse.RefId}",
                "PaymentSuccess",
                $"/dashboard/order/{order.Id}"
            );

            await _auditService.LogOrderEventAsync(order.Id, "PaymentVerified", order.UserId, $"RefID: {verificationResponse.RefId}");

            await dbTransaction.CommitAsync();

            return new PaymentVerificationResultDto
            {
                IsVerified = true,
                RedirectUrl = $"{frontendUrl}/payment/success?orderId={order.Id}&refId={verificationResponse.RefId}",
                RefId = verificationResponse.RefId,
                Message = "پرداخت با موفقیت انجام شد"
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Database transaction failed during payment completion for Order {OrderId}", order.Id);
            throw;
        }
    }

    public async Task ProcessGatewayWebhookAsync(string gatewayName, string authority, string status, long? refId)
    {
        await VerifyPaymentAsync(authority, status);
    }

    public async Task CleanupAbandonedPaymentsAsync(CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
        var pendingOrders = await _orderRepository.GetOrdersAsync(null, true, null, null, null, cutoffTime, 1, 100);

        var expiredOrders = pendingOrders.Orders
            .Where(o => o.OrderStatusId == 1 && !o.IsPaid)
            .ToList();

        foreach (var order in expiredOrders)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var transaction = order.PaymentTransactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

                if (transaction != null && transaction.Status == PaymentTransaction.PaymentStatuses.Pending)
                {
                    transaction.Status = PaymentTransaction.PaymentStatuses.Expired;
                }

                order.IsDeleted = true;
                order.DeletedBy = 0;
                order.DeletedAt = DateTime.UtcNow;

                await _auditService.LogOrderEventAsync(order.Id, "OrderExpired", 0, "Order expired due to non-payment.");
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired order {OrderId}", order.Id);
            }
        }
    }
}