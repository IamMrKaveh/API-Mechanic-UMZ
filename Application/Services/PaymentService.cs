namespace Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly LedkaContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;

    public PaymentService(
        IPaymentGateway paymentGateway,
        LedkaContext context,
        ILogger<PaymentService> logger,
        IHttpContextAccessor httpContextAccessor,
        IOptions<FrontendUrlsDto> frontendUrls)
    {
        _paymentGateway = paymentGateway;
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _frontendUrls = frontendUrls;
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
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString(),
            ["PaymentAuthority"] = authority,
            ["PaymentStatus"] = status
        }))
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .Include(t => t.Order)
                    .FirstOrDefaultAsync(t => t.Authority == authority);

                if (transaction == null)
                {
                    _logger.LogWarning("Payment transaction not found for authority: {Authority}", authority);
                    return new PaymentVerificationResultDto
                    {
                        IsVerified = false,
                        Message = "Transaction not found.",
                        RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/failure?reason=transaction_not_found"
                    };
                }

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["OrderId"] = transaction.OrderId,
                    ["UserId"] = transaction.UserId
                }))
                {
                    if (transaction.Status == PaymentTransaction.PaymentStatuses.Success)
                    {
                        return new PaymentVerificationResultDto
                        {
                            IsVerified = true,
                            RefId = transaction.RefId,
                            Message = "Payment already verified.",
                            RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/success?orderId={transaction.OrderId}&refId={transaction.RefId}"
                        };
                    }

                    if (status != "OK")
                    {
                        transaction.Status = PaymentTransaction.PaymentStatuses.Failed;
                        transaction.ErrorMessage = "Payment was cancelled or failed by user.";
                        transaction.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        return new PaymentVerificationResultDto
                        {
                            IsVerified = false,
                            Message = "Payment was cancelled.",
                            RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/failure?orderId={transaction.OrderId}&reason=cancelled"
                        };
                    }

                    transaction.Status = PaymentTransaction.PaymentStatuses.VerificationInProgress;
                    transaction.VerificationAttemptedAt = DateTime.UtcNow;
                    transaction.VerificationCount++;
                    await _context.SaveChangesAsync();

                    var retryPolicy = Policy
                        .Handle<HttpRequestException>()
                        .Or<TimeoutException>()
                        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                            (exception, timeSpan, retryCount, context) =>
                            {
                                _logger.LogWarning("Payment verification retry {RetryCount} for Authority {Authority} due to {Exception}",
                                    retryCount, authority, exception.Message);
                            });

                    GatewayVerificationResultDto verification;
                    try
                    {
                        verification = await retryPolicy.ExecuteAsync(() =>
                            _paymentGateway.VerifyPaymentAsync(authority, (int)transaction.Amount));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "All retry attempts failed for verification of Authority {Authority}", authority);
                        verification = new GatewayVerificationResultDto
                        {
                            IsVerified = false,
                            Message = "Gateway verification failed after retries."
                        };
                    }

                    transaction.UpdatedAt = DateTime.UtcNow;
                    transaction.GatewayRawResponse = verification.RawResponse;

                    if (verification.IsVerified)
                    {
                        transaction.Status = PaymentTransaction.PaymentStatuses.Success;
                        transaction.RefId = verification.RefId;
                        transaction.CardPan = verification.CardPan;
                        transaction.CardHash = verification.CardHash;
                        transaction.Fee = verification.Fee;
                        transaction.VerifiedAt = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        return new PaymentVerificationResultDto
                        {
                            IsVerified = true,
                            RefId = verification.RefId,
                            Message = "Payment verified successfully.",
                            RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/success?orderId={transaction.OrderId}&refId={verification.RefId}"
                        };
                    }
                    else
                    {
                        transaction.Status = PaymentTransaction.PaymentStatuses.Failed;
                        transaction.ErrorMessage = verification.Message;

                        await _context.SaveChangesAsync();

                        return new PaymentVerificationResultDto
                        {
                            IsVerified = false,
                            Message = verification.Message ?? "Verification failed.",
                            RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/failure?orderId={transaction.OrderId}&reason={Uri.EscapeDataString(verification.Message ?? "verification_failed")}"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment verification failed for Authority {Authority}", authority);
                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    Message = "Verification exception.",
                    RedirectUrl = $"{_frontendUrls.Value.BaseUrl}/payment/failure?reason=internal_error"
                };
            }
        }
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