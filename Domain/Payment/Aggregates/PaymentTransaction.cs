using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Aggregates;

public sealed class PaymentTransaction : AggregateRoot<PaymentTransactionId>, IAuditable
{
    public Guid OrderId { get; private set; }
    public UserId UserId { get; private set; } = default!;
    public PaymentAuthority Authority { get; private set; } = null!;
    public PaymentGateway Gateway { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; } = null!;
    public long? RefId { get; private set; }
    public decimal Fee { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Description { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public bool IsVerificationInProgress { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private const int DefaultExpiryMinutes = 20;
    private const int MaxExpiryMinutes = 60;
    private const int MaxDescriptionLength = 500;

    private PaymentTransaction()
    { }

    private PaymentTransaction(
        PaymentTransactionId id,
        Guid orderId,
        UserId userId,
        PaymentAuthority authority,
        Money amount,
        PaymentGateway gateway,
        string? description,
        int expiryMinutes) : base(id)
    {
        OrderId = orderId;
        UserId = userId;
        Authority = authority;
        Amount = amount;
        Gateway = gateway;
        Status = PaymentStatus.Pending;
        Description = description?.Trim();
        ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        CreatedAt = DateTime.UtcNow;
        IsVerificationInProgress = false;
    }

    public static PaymentTransaction Initiate(
        Guid orderId,
        UserId userId,
        string authority,
        decimal amount,
        string gateway,
        string? description = null,
        int expiryMinutes = DefaultExpiryMinutes)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("شناسه سفارش الزامی است.", nameof(orderId));

        Guard.Against.Null(userId, nameof(userId));
        ValidateAmount(amount);
        ValidateDescription(description);
        ValidateExpiryMinutes(expiryMinutes);

        var authorityVo = PaymentAuthority.Create(authority);
        var gatewayVo = PaymentGateway.FromString(gateway);

        var transaction = new PaymentTransaction(
            PaymentTransactionId.NewId(),
            orderId,
            userId,
            authorityVo,
            Money.FromDecimal(amount),
            gatewayVo,
            description,
            expiryMinutes);

        transaction.RaiseDomainEvent(new PaymentInitiatedEvent(
            transaction.Id.Value,
            orderId,
            amount));

        return transaction;
    }

    public void MarkAsVerificationInProgress()
    {
        EnsureCanStartVerification();

        IsVerificationInProgress = true;
        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSuccess(long refId, decimal fee = 0)
    {
        EnsureCanSucceed();
        ValidateRefId(refId);
        ValidateFee(fee);

        Status = PaymentStatus.Success;
        RefId = refId;
        Fee = fee;
        VerifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsVerificationInProgress = false;

        RaiseDomainEvent(new PaymentSucceededEvent(
            Id.Value,
            OrderId,
            refId,
            userId: 0,
            Amount.Amount));
    }

    public void MarkAsFailed(string? errorMessage = null)
    {
        EnsureCanFail();

        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage ?? "خطای نامشخص";
        UpdatedAt = DateTime.UtcNow;
        IsVerificationInProgress = false;

        RaiseDomainEvent(new PaymentFailedEvent(
            Id.Value,
            OrderId,
            ErrorMessage));
    }

    public void Expire()
    {
        if (!CanExpire()) return;

        Status = PaymentStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
        IsVerificationInProgress = false;

        RaiseDomainEvent(new PaymentExpiredEvent(
            Id.Value,
            OrderId,
            Amount.Amount,
            Authority));
    }

    public void Cancel(string? reason = null)
    {
        EnsureCanCancel();

        Status = PaymentStatus.Cancelled;
        ErrorMessage = reason ?? "لغو شده توسط کاربر";
        UpdatedAt = DateTime.UtcNow;
        IsVerificationInProgress = false;

        RaiseDomainEvent(new PaymentCancelledEvent(
            Id.Value,
            OrderId,
            ErrorMessage));
    }

    public void Refund(string? reason = null)
    {
        EnsureCanRefund();

        Status = PaymentStatus.Refunded;
        ErrorMessage = reason ?? "بازگشت وجه";
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new PaymentRefundedEvent(
            Id.Value,
            OrderId,
            0,
            Amount.Amount,
            ErrorMessage));
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt && Status == PaymentStatus.Pending;

    public bool IsSuccessful() => Status == PaymentStatus.Success;

    public bool IsPending() => Status == PaymentStatus.Pending;

    public bool IsFailed() => Status == PaymentStatus.Failed;

    public bool IsRefunded() => Status == PaymentStatus.Refunded;

    public bool IsCancelled() => Status == PaymentStatus.Cancelled;

    public bool CanBeVerified() =>
        (Status == PaymentStatus.Pending || Status == PaymentStatus.Processing) && !IsExpired();

    public bool CanExpire() =>
        Status == PaymentStatus.Pending || Status == PaymentStatus.Processing;

    public TimeSpan? GetTimeUntilExpiry()
    {
        if (Status != PaymentStatus.Pending) return null;
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public bool HasRefId() => RefId.HasValue;

    public bool MatchesAmount(decimal amount) => Math.Abs(Amount.Amount - amount) < 1;

    private void EnsureCanStartVerification()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("فقط تراکنش‌های در انتظار قابل بررسی هستند.");

        if (IsExpired())
            throw new Exceptions.PaymentExpiredException(Authority, ExpiresAt);
    }

    private void EnsureCanSucceed()
    {
        if (Status == PaymentStatus.Success)
            throw new Exceptions.PaymentAlreadyVerifiedException(Id.Value, RefId ?? 0);

        if (Status == PaymentStatus.Failed)
            throw new DomainException("امکان تأیید تراکنش ناموفق وجود ندارد.");

        if (Status == PaymentStatus.Expired)
            throw new Exceptions.PaymentExpiredException(Authority, ExpiresAt);
    }

    private void EnsureCanFail()
    {
        if (Status == PaymentStatus.Success)
            throw new DomainException("امکان تغییر وضعیت تراکنش موفق وجود ندارد.");
    }

    private void EnsureCanCancel()
    {
        if (Status == PaymentStatus.Success)
            throw new DomainException("امکان لغو تراکنش موفق وجود ندارد.");
    }

    private void EnsureCanRefund()
    {
        if (Status != PaymentStatus.Success)
            throw new DomainException("فقط تراکنش‌های موفق قابل بازگشت هستند.");
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new Exceptions.InvalidPaymentAmountException(0, amount);
    }

    private static void ValidateDescription(string? description)
    {
        if (!string.IsNullOrEmpty(description) && description.Length > MaxDescriptionLength)
            throw new DomainException($"توضیحات نمی‌تواند بیش از {MaxDescriptionLength} کاراکتر باشد.");
    }

    private static void ValidateExpiryMinutes(int expiryMinutes)
    {
        if (expiryMinutes <= 0)
            throw new DomainException("مدت انقضا باید بزرگتر از صفر باشد.");

        if (expiryMinutes > MaxExpiryMinutes)
            throw new DomainException($"مدت انقضا نمی‌تواند بیش از {MaxExpiryMinutes} دقیقه باشد.");
    }

    private static void ValidateRefId(long refId)
    {
        if (refId <= 0)
            throw new DomainException("کد پیگیری نامعتبر است.");
    }

    private static void ValidateFee(decimal fee)
    {
        if (fee < 0)
            throw new DomainException("کارمزد نمی‌تواند منفی باشد.");
    }
}