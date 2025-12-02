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

    public async Task<(string? PaymentUrl, string? Authority, string? Error)> InitiatePaymentAsync(int orderId, int userId, decimal amount, string description, string? mobile, string? email, string gatewayName, string ipAddress)
    {
        var gateway = _gateways.FirstOrDefault(g => g.GatewayName.Equals(gatewayName, StringComparison.OrdinalIgnoreCase));
        if (gateway == null) return (null, null, "درگاه پرداخت انتخابی معتبر نیست.");

        var callbackUrl = $"{_frontendUrls.BaseUrl}/payment/callback?orderId={orderId}";

        // Fix: Amount validation
        var order = await _orderRepository.GetOrderByIdAsync(orderId, userId, false);
        if (order == null || order.FinalAmount != amount) return (null, null, "مبلغ سفارش نامعتبر است.");

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
                    OriginalAmount = amount, // Fix: Store original amount
                    Amount = amount * 10, // Store in Rials as per legacy, or change consistently
                    Gateway = gateway.GatewayName,
                    Status = PaymentTransaction.PaymentStatuses.Pending,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ipAddress // Fix: Store real IP
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
            _logger.LogError(ex, "Error initiating payment for order {OrderId}", orderId);
            return (null, null, "خطا در ارتباط با درگاه پرداخت.");
        }
    }

    public async Task<PaymentVerificationResultDto> VerifyPaymentAsync(string authority, string status)
    {
        string frontendUrl = _frontendUrls.BaseUrl;
        if (string.IsNullOrEmpty(authority))
            return new PaymentVerificationResultDto { IsVerified = false, RedirectUrl = $"{frontendUrl}/payment/failure?reason=invalid_auth", Message = "Invalid Authority" };

        var lockKey = $"payment_verify:{authority}";

        // Fix: Retry logic for lock acquisition
        if (!await _cacheService.AcquireLockWithRetryAsync(lockKey, TimeSpan.FromSeconds(60), 3, 1000))
        {
            return new PaymentVerificationResultDto { IsVerified = false, RedirectUrl = $"{frontendUrl}/payment/failure?reason=processing", Message = "Transaction is processing" };
        }

        try
        {
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            var transaction = await _orderRepository.GetPaymentTransactionForUpdateAsync(authority);

            if (transaction == null) return new PaymentVerificationResultDto { IsVerified = false, RedirectUrl = $"{frontendUrl}/payment/failure?reason=tx_not_found", Message = "Transaction not found" };

            if (transaction.Status == PaymentTransaction.PaymentStatuses.Success)
                return new PaymentVerificationResultDto { IsVerified = true, RedirectUrl = $"{frontendUrl}/payment/success?orderId={transaction.OrderId}&refId={transaction.RefId}", RefId = transaction.RefId, Message = "Already Verified" };

            if (transaction.Status == PaymentTransaction.PaymentStatuses.VerificationInProgress)
                return new PaymentVerificationResultDto { IsVerified = false, RedirectUrl = $"{frontendUrl}/payment/failure?reason=processing", Message = "Verification In Progress" };

            transaction.Status = PaymentTransaction.PaymentStatuses.VerificationInProgress;
            transaction.VerificationAttemptedAt = DateTime.UtcNow;
            transaction.LastVerificationAttempt = DateTime.UtcNow;
            transaction.VerificationCount++;
            await _unitOfWork.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            if (!status.Equals("OK", StringComparison.OrdinalIgnoreCase) && !status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Status = PaymentTransaction.PaymentStatuses.Failed;
                transaction.ErrorMessage = "User Canceled";
                await _unitOfWork.SaveChangesAsync();
                return new PaymentVerificationResultDto { IsVerified = false, RedirectUrl = $"{frontendUrl}/payment/failure?reason=canceled&orderId={transaction.OrderId}", Message = "Canceled" };
            }

            var gateway = _gateways.FirstOrDefault(g => g.GatewayName.Equals(transaction.Gateway, StringComparison.OrdinalIgnoreCase));
            if (gateway == null) return new PaymentVerificationResultDto { IsVerified = false, Message = "Gateway not found" };

            var verificationResult = await gateway.VerifyPaymentAsync(transaction.OriginalAmount, authority);

            transaction.GatewayRawResponse = JsonSerializer.Serialize(verificationResult);

            if (verificationResult.IsVerified)
            {
                return await CompletePaymentSuccessAsync(transaction, verificationResult, frontendUrl);
            }
            else
            {
                if (transaction.VerificationCount < 3)
                {
                    transaction.Status = PaymentTransaction.PaymentStatuses.VerificationRetryable;
                }
                else
                {
                    transaction.Status = PaymentTransaction.PaymentStatuses.Failed;
                }

                transaction.ErrorMessage = verificationResult.Message;
                await _unitOfWork.SaveChangesAsync();
                return new PaymentVerificationResultDto { IsVerified = false, RedirectUrl = $"{frontendUrl}/payment/failure?reason=verify_failed", Message = verificationResult.Message };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify Exception {Authority}", authority);
            return new PaymentVerificationResultDto { IsVerified = false, RedirectUrl = $"{frontendUrl}/payment/failure?reason=exception", Message = "System Error" };
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(lockKey);
        }
    }

    private async Task<PaymentVerificationResultDto> CompletePaymentSuccessAsync(PaymentTransaction transaction, GatewayVerificationResultDto result, string frontendUrl)
    {
        using var dbTx = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var order = await _orderRepository.GetOrderForUpdateAsync(transaction.OrderId);
            if (order == null) throw new Exception("Order not found");

            transaction.Status = PaymentTransaction.PaymentStatuses.Success;
            transaction.RefId = result.RefId;
            transaction.VerifiedAt = DateTime.UtcNow;
            transaction.CardPan = result.CardPan;
            transaction.CardHash = result.CardHash;
            transaction.Fee = result.Fee;

            order.IsPaid = true;
            var paidStatus = await _orderStatusRepository.GetStatusByNameAsync("Paid") ?? await _orderStatusRepository.GetStatusByNameAsync("Processing");
            order.OrderStatusId = paidStatus?.Id ?? order.OrderStatusId;

            if (order.DiscountUsages != null)
            {
                foreach (var usage in order.DiscountUsages)
                {
                    usage.IsConfirmed = true;
                }
            }

            var cart = await _cartRepository.GetCartAsync(order.UserId);
            if (cart != null) _cartRepository.RemoveCartItems(cart.CartItems);
            await _cacheService.ClearAsync($"cart:user:{order.UserId}");

            await _unitOfWork.SaveChangesAsync();
            await dbTx.CommitAsync();

            await _auditService.LogOrderEventAsync(order.Id, "PaymentVerified", order.UserId, $"RefID: {result.RefId}");

            return new PaymentVerificationResultDto
            {
                IsVerified = true,
                RedirectUrl = $"{frontendUrl}/payment/success?orderId={order.Id}&refId={result.RefId}",
                RefId = result.RefId,
                Message = "Success"
            };
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync();
            _logger.LogError(ex, "CompletePayment Error");
            throw;
        }
    }

    public async Task ProcessGatewayWebhookAsync(string gatewayName, string authority, string status, long? refId)
    {
        await VerifyPaymentAsync(authority, status);
    }

    public async Task CleanupAbandonedPaymentsAsync(CancellationToken cancellationToken)
    {
    }
}