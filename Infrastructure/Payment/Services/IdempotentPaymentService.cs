namespace Infrastructure.Payment.Services;

/// <summary>
/// سرویس پرداخت Idempotent با پشتیبانی از:
/// - Idempotency Key (جلوگیری از درخواست‌های تکراری)
/// - Retry مدیریت‌شده
/// - Payment Gateway Abstraction
/// </summary>
public sealed class IdempotentPaymentService : IPaymentService
{
    private static readonly TimeSpan IdempotencyWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan GatewayTimeout = TimeSpan.FromSeconds(10);

    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IPaymentTransactionRepository _transactionRepo;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IdempotentPaymentService> _logger;

    public IdempotentPaymentService(
        IPaymentGatewayFactory gatewayFactory,
        IPaymentTransactionRepository transactionRepo,
        ICacheService cache,
        IUnitOfWork unitOfWork,
        ILogger<IdempotentPaymentService> logger)
    {
        _gatewayFactory = gatewayFactory;
        _transactionRepo = transactionRepo;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ─── Initiate Payment ────────────────────────────────────────────────────

    public async Task<PaymentInitiationResultDto> InitiatePaymentAsync(
        PaymentInitiationDto dto,
        CancellationToken ct = default)
    {
        var idempotencyKey = BuildIdempotencyKey(dto.OrderId, dto.Amount.Amount);

        // بررسی وجود درخواست تکراری در Cache
        var cachedResult = await _cache.GetAsync<PaymentInitiationResultDto>(idempotencyKey);
        if (cachedResult is not null)
        {
            _logger.LogInformation(
                "Idempotent payment request hit for Order {OrderId}. Returning cached result.",
                dto.OrderId);
            return cachedResult;
        }

        // بررسی وجود تراکنش فعال در DB
        var existingTransaction = await _transactionRepo
            .GetActiveByOrderIdAsync(dto.OrderId, ct);

        if (existingTransaction is not null)
        {
            var cached = new PaymentInitiationResultDto
            {
                IsSuccess = true,
                Authority = existingTransaction.Authority,
                PaymentUrl = BuildPaymentUrl(existingTransaction.Authority),
                Message = "تراکنش فعال موجود است"
            };
            await _cache.SetAsync(idempotencyKey, cached, IdempotencyWindow);
            return cached;
        }

        // انتخاب درگاه و ارسال درخواست
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
            return new PaymentInitiationResultDto
            {
                IsSuccess = false,
                Message = "درگاه پرداخت پاسخ نداد. لطفاً دوباره تلاش کنید."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment gateway error for Order {OrderId}", dto.OrderId);
            return new PaymentInitiationResultDto
            {
                IsSuccess = false,
                Message = "خطا در ارتباط با درگاه پرداخت."
            };
        }

        if (!gatewayResult.IsSuccess)
        {
            return new PaymentInitiationResultDto
            {
                IsSuccess = false,
                Message = gatewayResult.Message ?? "خطا در درگاه پرداخت."
            };
        }

        // ذخیره تراکنش در DB
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

        var result = new PaymentInitiationResultDto
        {
            IsSuccess = true,
            Authority = gatewayResult.Authority,
            PaymentUrl = gatewayResult.PaymentUrl ?? gatewayResult.RedirectUrl,
            Message = "موفق"
        };

        // Cache کردن نتیجه برای Idempotency
        await _cache.SetAsync(idempotencyKey, result, IdempotencyWindow);

        return result;
    }

    // ─── Verify Payment ──────────────────────────────────────────────────────

    public async Task<ServiceResult<PaymentVerificationResultDto>> VerifyPaymentAsync(
        string authority,
        int amount,
        CancellationToken ct = default)
    {
        var verifyIdempotencyKey = $"payment:verify:{authority}";

        // جلوگیری از double-verification
        var cached = await _cache.GetAsync<PaymentVerificationResultDto>(verifyIdempotencyKey);
        if (cached is not null)
        {
            _logger.LogWarning(
                "Duplicate verify attempt for authority {Authority}. Returning cached result.",
                authority);
            return ServiceResult<PaymentVerificationResultDto>.Success(cached);
        }

        // بررسی وضعیت تراکنش در DB
        var transaction = await _transactionRepo.GetByAuthorityAsync(authority, ct);
        if (transaction is null)
            return ServiceResult<PaymentVerificationResultDto>.Failure("تراکنش یافت نشد.");

        if (transaction.IsSuccessful())
        {
            // قبلاً تأیید شده - ایمن برای Retry
            var existing = new PaymentVerificationResultDto
            {
                IsVerified = true,
                RefId = transaction.RefId,
                CardPan = transaction.CardPan,
                Message = "قبلاً تأیید شده"
            };
            return ServiceResult<PaymentVerificationResultDto>.Success(existing);
        }

        // ارسال درخواست تأیید به درگاه
        var gateway = _gatewayFactory.GetGateway(transaction.Gateway);
        var gatewayResult = await gateway.VerifyPaymentAsync(authority, amount);

        if (!gatewayResult.IsVerified)
        {
            transaction.MarkAsFailed(gatewayResult.Message ?? "تأیید ناموفق");
            _transactionRepo.Update(transaction);
            await _unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<PaymentVerificationResultDto>.Failure(
                gatewayResult.Message ?? "پرداخت تأیید نشد.");
        }

        transaction.MarkAsSuccess(gatewayResult.RefId!.Value, gatewayResult.CardPan);
        _transactionRepo.Update(transaction);
        await _unitOfWork.SaveChangesAsync(ct);

        var verifyResult = new PaymentVerificationResultDto
        {
            IsVerified = true,
            RefId = gatewayResult.RefId,
            CardPan = gatewayResult.CardPan,
            Message = "پرداخت موفق"
        };

        // Cache کردن نتیجه تأیید برای 24 ساعت (Idempotency)
        await _cache.SetAsync(verifyIdempotencyKey, verifyResult, IdempotencyWindow);

        return ServiceResult<PaymentVerificationResultDto>.Success(verifyResult);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string BuildIdempotencyKey(int orderId, decimal amount) =>
        $"payment:initiate:{orderId}:{amount:F0}";

    private static string BuildPaymentUrl(string? authority) =>
        authority is null ? string.Empty :
        $"https://www.zarinpal.com/pg/StartPay/{authority}";
}