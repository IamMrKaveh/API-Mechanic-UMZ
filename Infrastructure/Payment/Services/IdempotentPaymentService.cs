namespace Infrastructure.Payment.Services;

public sealed class PaymentService : IPaymentService
{
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan GatewayTimeout = TimeSpan.FromSeconds(10);

    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IPaymentTransactionRepository _transactionRepo;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentGatewayFactory gatewayFactory,
        IPaymentTransactionRepository transactionRepo,
        ICacheService cache,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger
        )
    {
        _gatewayFactory = gatewayFactory;
        _transactionRepo = transactionRepo;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private record CachedPaymentInitiation(bool IsSuccess, string? Authority, string? PaymentUrl, string? Message);
    private record CachedPaymentVerification(bool IsVerified, long? RefId, string? CardPan, string? Message);

    public async Task<ServiceResult<(bool IsSuccess, string? Authority, string? PaymentUrl, string? Message)>> InitiatePaymentAsync(
        PaymentInitiationDto dto,
        CancellationToken ct = default
        )
    {
        var idempotencyKey = BuildIdempotencyKey(dto.OrderId, dto.Amount.Amount);

        var cachedResult = await _cache.GetAsync<CachedPaymentInitiation>(idempotencyKey);
        if (cachedResult is not null)
        {
            _logger.LogInformation(
                "Idempotent payment request hit for Order {OrderId}. Returning cached result.",
                dto.OrderId);
            return ServiceResult<(bool, string?, string?, string?)>.Success((cachedResult.IsSuccess, cachedResult.Authority, cachedResult.PaymentUrl, cachedResult.Message));
        }

        var existingTransaction = await _transactionRepo
            .GetActiveByOrderIdAsync(dto.OrderId, ct);

        if (existingTransaction is not null)
        {
            var cached = new CachedPaymentInitiation(
                true,
                existingTransaction.Authority,
                BuildPaymentUrl(existingTransaction.Authority),
                "تراکنش فعال موجود است"
            );
            await _cache.SetAsync(idempotencyKey, cached, IdempotencyWindow);
            return ServiceResult<(bool, string?, string?, string?)>.Success((cached.IsSuccess, cached.Authority, cached.PaymentUrl, cached.Message));
        }

        var gateway = _gatewayFactory.GetDefaultGateway();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(GatewayTimeout);

        PaymentRequestResultDto gatewayResult;
        try
        {
            gatewayResult = await gateway.RequestPaymentAsync(
                (int)dto.Amount.Amount,
                dto.Description,
                dto.CallbackUrl,
                dto.Mobile,
                dto.Email);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Payment gateway {Gateway} timed out for Order {OrderId}",
                gateway.GatewayName, dto.OrderId);
            return ServiceResult<(bool, string?, string?, string?)>.Success((false, null, null, "درگاه پرداخت پاسخ نداد. لطفاً دوباره تلاش کنید."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment gateway error for Order {OrderId}", dto.OrderId);
            return ServiceResult<(bool, string?, string?, string?)>.Success((false, null, null, "خطا در ارتباط با درگاه پرداخت."));
        }

        if (!gatewayResult.IsSuccess)
        {
            return ServiceResult<(bool, string?, string?, string?)>.Success((false, null, null, gatewayResult.Message ?? "خطا در درگاه پرداخت."));
        }

        var transaction = PaymentTransaction.Initiate(
                    dto.OrderId,
                    dto.UserId,
                    gatewayResult.Authority!,
                    dto.Amount.Amount,
                    gateway.GatewayName,
                    dto.Description,
                    "Unknown",
                    null, 20);

        await _transactionRepo.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var result = new CachedPaymentInitiation(
            true,
            gatewayResult.Authority,
            gatewayResult.PaymentUrl ?? gatewayResult.RedirectUrl,
            "موفق"
        );

        await _cache.SetAsync(idempotencyKey, result, IdempotencyWindow);

        return ServiceResult<(bool, string?, string?, string?)>.Success((result.IsSuccess, result.Authority, result.PaymentUrl, result.Message));
    }

    public async Task<ServiceResult<(bool IsVerified, long? RefId, string? CardPan, string? Message)>> VerifyPaymentAsync(
        string authority,
        int amount,
        CancellationToken ct = default
        )
    {
        var verifyIdempotencyKey = $"payment:verify:{authority}";

        var cached = await _cache.GetAsync<CachedPaymentVerification>(verifyIdempotencyKey);
        if (cached is not null)
        {
            _logger.LogWarning(
                "Duplicate verify attempt for authority {Authority}. Returning cached result.",
                authority);
            return ServiceResult<(bool, long?, string?, string?)>.Success((cached.IsVerified, cached.RefId, cached.CardPan, cached.Message));
        }

        var transaction = await _transactionRepo.GetByAuthorityAsync(authority, ct);
        if (transaction is null)
            return ServiceResult<(bool, long?, string?, string?)>.Failure("تراکنش یافت نشد.");

        if (transaction.IsSuccessful())
        {
            var existing = new CachedPaymentVerification(
                true,
                transaction.RefId,
                transaction.CardPan,
                "قبلاً تأیید شده"
            );
            return ServiceResult<(bool, long?, string?, string?)>.Success((existing.IsVerified, existing.RefId, existing.CardPan, existing.Message));
        }

        var gateway = _gatewayFactory.GetGateway(transaction.Gateway);
        var gatewayResult = await gateway.VerifyPaymentAsync(authority, amount);

        if (!gatewayResult.IsVerified)
        {
            transaction.MarkAsFailed(gatewayResult.Message ?? "تأیید ناموفق");
            _transactionRepo.Update(transaction);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<(bool, long?, string?, string?)>.Failure(gatewayResult.Message ?? "پرداخت تأیید نشد.");
        }

        transaction.MarkAsSuccess(gatewayResult.RefId!.Value, gatewayResult.CardPan);
        _transactionRepo.Update(transaction);
        await _unitOfWork.SaveChangesAsync(ct);

        var verifyResult = new CachedPaymentVerification(
            true,
            gatewayResult.RefId,
            gatewayResult.CardPan,
            "پرداخت موفق"
        );

        await _cache.SetAsync(verifyIdempotencyKey, verifyResult, IdempotencyWindow);

        return ServiceResult<(bool, long?, string?, string?)>.Success((verifyResult.IsVerified, verifyResult.RefId, verifyResult.CardPan, verifyResult.Message));
    }

    private static string BuildIdempotencyKey(int orderId, decimal amount) =>
        $"payment:initiate:{orderId}:{amount:F0}";

    private static string BuildPaymentUrl(string? authority) =>
        authority is null ? string.Empty :
        $"https://www.zarinpal.com/pg/StartPay/{authority}";
}