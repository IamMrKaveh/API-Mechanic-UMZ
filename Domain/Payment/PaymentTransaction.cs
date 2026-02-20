using Domain.Payment.Events;
using Domain.Payment.ValueObjects;

namespace Domain.Payment;

public class PaymentTransaction : AggregateRoot, ISoftDeletable, IAuditable
{
    private int _orderId;
    private int _userId;
    private string _authority = null!;
    private Money _amount = null!;
    private string _gateway = null!;
    private PaymentStatus _status = null!;
    private long? _refId;
    private string? _cardPan;
    private string? _cardHash;
    private decimal _fee;
    private string? _ipAddress;
    private string? _errorMessage;
    private string? _description;
    private string? _rawRequest;
    private string? _rawResponse;
    private DateTime? _verifiedAt;
    private bool _isVerificationInProgress;
    private DateTime _expiresAt;

    public int OrderId => _orderId;
    public int UserId => _userId;
    public string Authority => _authority;
    public Money Amount => _amount;
    public string Gateway => _gateway;
    public PaymentStatus Status => _status;
    public long? RefId => _refId;
    public string? CardPan => _cardPan;
    public string? CardHash => _cardHash;
    public decimal Fee => _fee;
    public string? IpAddress => _ipAddress;
    public string? ErrorMessage => _errorMessage;
    public string? Description => _description;
    public string? RawRequest => _rawRequest;
    public string? RawResponse => _rawResponse;
    public DateTime? VerifiedAt => _verifiedAt;
    public bool IsVerificationInProgress => _isVerificationInProgress;
    public DateTime ExpiresAt => _expiresAt;

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation
    public Order.Order? Order { get; private set; }

    // Business Constants
    private const int DefaultExpiryMinutes = 20;

    private const int MaxAuthorityLength = 100;
    private const int MaxGatewayLength = 50;
    private const int MaxCardPanLength = 20;
    private const int MaxDescriptionLength = 500;

    #region Factory Methods

    public static PaymentTransaction Initiate(
        int orderId,
        int userId,
        string authority,
        decimal amount,
        string gateway,
        string? description = null,
        string? ipAddress = null,
        string? rawRequest = null,
        int expiryMinutes = DefaultExpiryMinutes)
    {
        Guard.Against.NegativeOrZero(orderId, nameof(orderId));
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        ValidateAuthority(authority);
        ValidateAmount(amount);
        ValidateGateway(gateway);
        ValidateDescription(description);
        ValidateExpiryMinutes(expiryMinutes);

        var transaction = new PaymentTransaction
        {
            _orderId = orderId,
            _userId = userId,
            _authority = authority.Trim(),
            _amount = Money.FromDecimal(amount),
            _gateway = gateway.Trim(),
            _status = PaymentStatus.Pending,
            _description = description?.Trim(),
            _ipAddress = ipAddress,
            _rawRequest = rawRequest,
            _expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            CreatedAt = DateTime.UtcNow,
            _isVerificationInProgress = false
        };

        transaction.AddDomainEvent(new Events.PaymentInitiatedEvent(
            transaction.Id,
            orderId,
            amount));

        return transaction;
    }

    #endregion Factory Methods

    #region State Transitions

    /// <summary>
    /// شروع فرآیند تأیید - تغییر وضعیت به Processing
    /// </summary>
    public void MarkAsVerificationInProgress()
    {
        EnsureCanStartVerification();

        _isVerificationInProgress = true;
        _status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// ثبت موفقیت پرداخت با کد پیگیری
    /// </summary>
    public void MarkAsSuccess(
        long refId,
        string? cardPan = null,
        string? cardHash = null,
        decimal fee = 0,
        string? rawResponse = null)
    {
        EnsureCanSucceed();
        ValidateRefId(refId);
        ValidateCardPan(cardPan);
        ValidateFee(fee);

        _status = PaymentStatus.Success;
        _refId = refId;
        _cardPan = cardPan;
        _cardHash = cardHash;
        _fee = fee;
        _rawResponse = rawResponse;
        _verifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        _isVerificationInProgress = false;

        AddDomainEvent(new Events.PaymentSucceededEvent(Id, _orderId, refId, _userId));
    }

    /// <summary>
    /// ثبت شکست پرداخت با پیام خطا
    /// </summary>
    public void MarkAsFailed(string? errorMessage = null, string? rawResponse = null)
    {
        EnsureCanFail();

        _status = PaymentStatus.Failed;
        _errorMessage = errorMessage;
        _rawResponse = rawResponse;
        UpdatedAt = DateTime.UtcNow;
        _isVerificationInProgress = false;

        AddDomainEvent(new Events.PaymentFailedEvent(Id, _orderId, errorMessage ?? "خطای نامشخص"));
    }

    /// <summary>
    /// منقضی کردن تراکنش
    /// </summary>
    public void Expire()
    {
        if (!CanExpire())
            return;

        _status = PaymentStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
        _isVerificationInProgress = false;

        AddDomainEvent(new Events.PaymentExpiredEvent(Id, _orderId, _amount.Amount, _authority));
    }

    /// <summary>
    /// لغو تراکنش توسط کاربر
    /// </summary>
    public void Cancel(string? reason = null)
    {
        EnsureCanCancel();

        _status = PaymentStatus.Cancelled;
        _errorMessage = reason ?? "لغو شده توسط کاربر";
        UpdatedAt = DateTime.UtcNow;
        _isVerificationInProgress = false;
    }

    /// <summary>
    /// استرداد وجه تراکنش موفق
    /// </summary>
    public void Refund(string? reason = null)
    {
        EnsureCanRefund();

        _status = PaymentStatus.Refunded;
        _errorMessage = reason ?? "بازگشت وجه";
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentRefundedEvent(Id, _orderId, _amount.Amount, reason));
    }

    #endregion State Transitions

    #region Query Methods

    public bool IsExpired() => DateTime.UtcNow > _expiresAt && _status == PaymentStatus.Pending;

    public bool IsSuccessful() => _status == PaymentStatus.Success;

    public bool IsPending() => _status == PaymentStatus.Pending;

    public bool IsFailed() => _status == PaymentStatus.Failed;

    public bool IsRefunded() => _status == PaymentStatus.Refunded;

    public bool IsCancelled() => _status == PaymentStatus.Cancelled;

    public bool CanBeVerified() =>
        (_status == PaymentStatus.Pending || _status == PaymentStatus.Processing) &&
        !IsExpired();

    public bool CanExpire() =>
        _status == PaymentStatus.Pending || _status == PaymentStatus.Processing;

    public TimeSpan? GetTimeUntilExpiry()
    {
        if (_status != PaymentStatus.Pending)
            return null;

        var remaining = _expiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    public bool HasRefId() => _refId.HasValue;

    public bool MatchesAmount(decimal amount) => Math.Abs(_amount.Amount - amount) < 1;

    #endregion Query Methods

    #region Soft Delete

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    #endregion Soft Delete

    #region Domain Invariants

    private void EnsureCanStartVerification()
    {
        if (_status != PaymentStatus.Pending)
            throw new DomainException("فقط تراکنش‌های در انتظار قابل بررسی هستند.");

        if (IsExpired())
            throw new Exceptions.PaymentExpiredException(_authority, _expiresAt);
    }

    private void EnsureCanSucceed()
    {
        if (_status == PaymentStatus.Success)
            throw new Exceptions.PaymentAlreadyVerifiedException(Id, _refId ?? 0);

        if (_status == PaymentStatus.Failed)
            throw new DomainException("امکان تأیید تراکنش ناموفق وجود ندارد.");

        if (_status == PaymentStatus.Expired)
            throw new Exceptions.PaymentExpiredException(_authority, _expiresAt);
    }

    private void EnsureCanFail()
    {
        if (_status == PaymentStatus.Success)
            throw new DomainException("امکان تغییر وضعیت تراکنش موفق وجود ندارد.");
    }

    private void EnsureCanCancel()
    {
        if (_status == PaymentStatus.Success)
            throw new DomainException("امکان لغو تراکنش موفق وجود ندارد.");
    }

    private void EnsureCanRefund()
    {
        if (_status != PaymentStatus.Success)
            throw new DomainException("فقط تراکنش‌های موفق قابل بازگشت هستند.");
    }

    private static void ValidateAuthority(string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            throw new DomainException("شناسه پرداخت الزامی است.");

        if (authority.Trim().Length > MaxAuthorityLength)
            throw new DomainException($"شناسه پرداخت نمی‌تواند بیش از {MaxAuthorityLength} کاراکتر باشد.");
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new Exceptions.InvalidPaymentAmountException(0, amount);
    }

    private static void ValidateGateway(string gateway)
    {
        if (string.IsNullOrWhiteSpace(gateway))
            throw new DomainException("درگاه پرداخت الزامی است.");

        if (gateway.Trim().Length > MaxGatewayLength)
            throw new DomainException($"نام درگاه نمی‌تواند بیش از {MaxGatewayLength} کاراکتر باشد.");
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

        if (expiryMinutes > 60)
            throw new DomainException("مدت انقضا نمی‌تواند بیش از ۶۰ دقیقه باشد.");
    }

    private static void ValidateRefId(long refId)
    {
        if (refId <= 0)
            throw new DomainException("کد پیگیری نامعتبر است.");
    }

    private static void ValidateCardPan(string? cardPan)
    {
        if (!string.IsNullOrEmpty(cardPan) && cardPan.Length > MaxCardPanLength)
            throw new DomainException($"شماره کارت نمی‌تواند بیش از {MaxCardPanLength} کاراکتر باشد.");
    }

    private static void ValidateFee(decimal fee)
    {
        if (fee < 0)
            throw new DomainException("کارمزد نمی‌تواند منفی باشد.");
    }

    #endregion Domain Invariants
}