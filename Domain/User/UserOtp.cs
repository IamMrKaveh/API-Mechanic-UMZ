namespace Domain.User;

public class UserOtp : BaseEntity, IAuditable
{
    private string _otpHash = null!;
    private DateTime _expiresAt;
    private bool _isUsed;
    private DateTime? _usedAt;
    private int _attemptCount;

    public int UserId { get; private set; }
    public string OtpHash => _otpHash;
    public DateTime ExpiresAt => _expiresAt;
    public bool IsUsed => _isUsed;
    public DateTime? UsedAt => _usedAt;
    public int AttemptCount => _attemptCount;

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public User? User { get; private set; }

    // Business Constants
    private const int MaxAttempts = 3;

    private const int DefaultExpiryMinutes = 2;
    private const int MaxExpiryMinutes = 10;

    private UserOtp()
    { }

    #region Factory Method

    public static UserOtp Create(int userId, string otpHash, int expiryMinutes = DefaultExpiryMinutes)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        if (string.IsNullOrWhiteSpace(otpHash))
            throw new DomainException("هش کد OTP الزامی است.");

        if (expiryMinutes <= 0)
            throw new DomainException("مدت اعتبار باید بزرگتر از صفر باشد.");

        if (expiryMinutes > MaxExpiryMinutes)
            throw new DomainException($"مدت اعتبار نمی‌تواند بیش از {MaxExpiryMinutes} دقیقه باشد.");

        return new UserOtp
        {
            UserId = userId,
            _otpHash = otpHash,
            _expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            _isUsed = false,
            _attemptCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion Factory Method

    #region Domain Behaviors

    public bool Verify(string otpHash)
    {
        if (_isUsed)
            return false;

        if (IsExpired())
            return false;

        _attemptCount++;
        UpdatedAt = DateTime.UtcNow;

        // حداکثر تعداد تلاش
        if (_attemptCount > MaxAttempts)
        {
            Invalidate();
            return false;
        }

        if (_otpHash != otpHash)
            return false;

        // موفقیت
        MarkAsUsed();
        return true;
    }

    public void IncrementAttempts()
    {
        if (_isUsed || IsExpired()) return;

        _attemptCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Invalidate()
    {
        if (_isUsed) return;

        _isUsed = true;
        _usedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private void MarkAsUsed()
    {
        _isUsed = true;
        _usedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Domain Behaviors

    #region Query Methods

    public bool IsExpired() => DateTime.UtcNow > _expiresAt;

    public bool IsValid() => !_isUsed && !IsExpired() && _attemptCount < MaxAttempts;

    public bool HasExceededMaxAttempts() => _attemptCount >= MaxAttempts;

    public int GetRemainingAttempts() => Math.Max(0, MaxAttempts - _attemptCount);

    public TimeSpan? GetRemainingTime()
    {
        if (IsExpired() || _isUsed)
            return null;

        return _expiresAt - DateTime.UtcNow;
    }

    public int GetRemainingSeconds()
    {
        var remaining = GetRemainingTime();
        return remaining.HasValue ? (int)remaining.Value.TotalSeconds : 0;
    }

    #endregion Query Methods
}