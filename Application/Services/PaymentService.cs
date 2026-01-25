using Application.Common.Interfaces.Persistence;

namespace Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly LedkaContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        IPaymentGateway paymentGateway,
        LedkaContext context,
        ILogger<PaymentService> logger,
        IHttpContextAccessor httpContextAccessor,
        IOptions<FrontendUrlsDto> frontendUrls,
        IUnitOfWork unitOfWork)
    {
        _paymentGateway = paymentGateway;
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _frontendUrls = frontendUrls;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentRequestResultDto> InitiatePaymentAsync(PaymentInitiationDto initiationDto)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString(),
            ["OrderId"] = initiationDto.OrderId,
            ["UserId"] = initiationDto.UserId
        }))
        {
            try
            {
                var result = await _paymentGateway.RequestPaymentAsync(
                    initiationDto.Amount,
                    initiationDto.Description,
                    initiationDto.CallbackUrl,
                    initiationDto.Mobile,
                    initiationDto.Email);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.Authority))
                {
                    var transaction = new PaymentTransaction
                    {
                        OrderId = initiationDto.OrderId,
                        UserId = initiationDto.UserId,
                        Authority = result.Authority,
                        Amount = initiationDto.Amount,
                        OriginalAmount = initiationDto.Amount,
                        Description = initiationDto.Description,
                        Gateway = _paymentGateway.GatewayName,
                        Status = PaymentTransaction.PaymentStatuses.Pending,
                        CreatedAt = DateTime.UtcNow,
                        IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        GatewayRawResponse = result.RawResponse
                    };

                    _context.PaymentTransactions.Add(transaction);
                    await _context.SaveChangesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment initiation failed for Order {OrderId}", initiationDto.OrderId);
                return new PaymentRequestResultDto { IsSuccess = false, Message = "Gateway error." };
            }
        }
    }

    public async Task<PaymentVerificationResultDto> VerifyPaymentAsync(string authority, string status)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var paymentTx = await _context.PaymentTransactions
                    .Include(t => t.Order)
                    .FirstOrDefaultAsync(t => t.Authority == authority);

                if (paymentTx == null)
                {
                    return new PaymentVerificationResultDto { IsVerified = false, Message = "تراکنش یافت نشد." };
                }

                if (paymentTx.Status == PaymentTransaction.PaymentStatuses.Success)
                {
                    return new PaymentVerificationResultDto
                    {
                        IsVerified = true,
                        RefId = paymentTx.RefId ?? 0,
                        Message = "تراکنش قبلاً تایید شده است.",
                        RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/result?status=success&refId={paymentTx.RefId}&orderId={paymentTx.OrderId}"
                    };
                }

                if (status.ToUpper() != "OK")
                {
                    paymentTx.Status = PaymentTransaction.PaymentStatuses.Failed;
                    paymentTx.ErrorMessage = "تراکنش توسط کاربر لغو شد یا ناموفق بود.";
                    paymentTx.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new PaymentVerificationResultDto
                    {
                        IsVerified = false,
                        Message = "پرداخت ناموفق بود.",
                        RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/result?status=failure&reason=cancelled"
                    };
                }

                var verification = await _paymentGateway.VerifyPaymentAsync(authority, (int)paymentTx.Amount);

                paymentTx.UpdatedAt = DateTime.UtcNow;
                paymentTx.GatewayRawResponse = verification.RawResponse;

                if (verification.IsVerified)
                {
                    paymentTx.Status = PaymentTransaction.PaymentStatuses.Success;
                    paymentTx.RefId = verification.RefId;
                    paymentTx.CardPan = verification.CardPan;
                    paymentTx.VerifiedAt = DateTime.UtcNow;

                    if (paymentTx.Order != null)
                    {
                        paymentTx.Order.IsPaid = true;
                        paymentTx.Order.PaymentDate = DateTime.UtcNow;
                        // Change status to Processing (Id: 2)
                        paymentTx.Order.OrderStatusId = 2;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new PaymentVerificationResultDto
                    {
                        IsVerified = true,
                        RefId = verification.RefId,
                        Message = "پرداخت با موفقیت انجام شد.",
                        RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/result?status=success&refId={verification.RefId}&orderId={paymentTx.OrderId}"
                    };
                }
                else
                {
                    paymentTx.Status = PaymentTransaction.PaymentStatuses.Failed;
                    paymentTx.ErrorMessage = verification.Message;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new PaymentVerificationResultDto
                    {
                        IsVerified = false,
                        Message = verification.Message,
                        RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/result?status=failure&reason={Uri.EscapeDataString(verification.Message ?? "Error")}"
                    };
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error verifying payment authority: {Authority}", authority);
                return new PaymentVerificationResultDto { IsVerified = false, Message = "خطای سیستمی رخ داده است." };
            }
        });
    }

    public async Task ProcessGatewayWebhookAsync(string gatewayName, string authority, string status, long? refId)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid().ToString(),
            ["GatewayName"] = gatewayName,
            ["PaymentAuthority"] = authority,
            ["PaymentRefId"] = refId ?? 0
        }))
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .Include(t => t.Order)
                    .FirstOrDefaultAsync(t => t.Authority == authority);

                if (transaction == null)
                {
                    _logger.LogWarning("Webhook received for unknown authority: {Authority}", authority);
                    return;
                }

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["OrderId"] = transaction.OrderId,
                    ["UserId"] = transaction.UserId
                }))
                {
                    if (transaction.Status == PaymentTransaction.PaymentStatuses.Success)
                    {
                        _logger.LogInformation("Webhook received for already verified transaction: {Authority}", authority);
                        return;
                    }

                    if (status == "OK" && refId.HasValue)
                    {
                        transaction.Status = PaymentTransaction.PaymentStatuses.Success;
                        transaction.RefId = refId.Value;
                        transaction.VerifiedAt = DateTime.UtcNow;
                        transaction.UpdatedAt = DateTime.UtcNow;

                        if (transaction.Order != null && !transaction.Order.IsPaid)
                        {
                            transaction.Order.IsPaid = true;
                            transaction.Order.PaymentDate = DateTime.UtcNow;

                            var paidStatus = await _context.OrderStatuses
                                .FirstOrDefaultAsync(s => s.Name == "Processing" || s.Name == "در حال پردازش");

                            if (paidStatus != null)
                            {
                                transaction.Order.OrderStatusId = paidStatus.Id;
                            }
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Webhook processed successfully for authority: {Authority}, RefId: {RefId}", authority, refId);
                    }
                    else
                    {
                        transaction.Status = PaymentTransaction.PaymentStatuses.Failed;
                        transaction.ErrorMessage = $"Webhook status: {status}";
                        transaction.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogWarning("Webhook indicated failed payment for authority: {Authority}", authority);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook for authority: {Authority}", authority);
            }
        }
    }

    public async Task CleanupAbandonedPaymentsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-30);

            var abandonedTransactions = await _context.PaymentTransactions
                .Where(t => t.Status == PaymentTransaction.PaymentStatuses.Pending &&
                           t.CreatedAt < cutoffTime)
                .ToListAsync(cancellationToken);

            foreach (var transaction in abandonedTransactions)
            {
                transaction.Status = PaymentTransaction.PaymentStatuses.Expired;
                transaction.ErrorMessage = "Payment session expired";
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            if (abandonedTransactions.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cleaned up {Count} abandoned payment transactions", abandonedTransactions.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during payment cleanup");
        }
    }
}