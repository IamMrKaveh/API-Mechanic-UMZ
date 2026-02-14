namespace Domain.User;

public class UserSession : BaseEntity, IAuditable
{
    private string _tokenSelector = null!;
    private string _tokenVerifierHash = null!;
    private DateTime _expiresAt;
    private bool _isRevoked;
    private DateTime? _revokedAt;
    private string _createdByIp = null!;
    private string? _userAgent;
    private string? _replacedByTokenHash;
    private string _sessionType = "Web";
    private DateTime? _lastActivityAt;

    public int UserId { get; private set; }
    public string TokenSelector { get => _tokenSelector; internal set => _tokenSelector = value; }
    public string TokenVerifierHash { get => _tokenVerifierHash; internal set => _tokenVerifierHash = value; }
    public DateTime ExpiresAt { get => _expiresAt; internal set => _expiresAt = value; }
    public bool IsRevoked => _isRevoked;
    public DateTime? RevokedAt { get => _revokedAt; internal set => _revokedAt = value; }
    public string CreatedByIp { get => _createdByIp; internal set => _createdByIp = value; }
    public string? UserAgent { get => _userAgent; internal set => _userAgent = value; }
    public string? ReplacedByTokenHash { get => _replacedByTokenHash; internal set => _replacedByTokenHash = value; }
    public string SessionType { get => _sessionType; internal set => _sessionType = value; }
    public DateTime? LastActivityAt { get => _lastActivityAt; internal set => _lastActivityAt = value; }

    // Computed Properties
    public bool IsActive => !_isRevoked && DateTime.UtcNow < _expiresAt;

    public bool IsExpired => DateTime.UtcNow >= _expiresAt;

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public User? User { get; private set; }

    // Business Constants
    private const int DefaultExpiryDays = 30;

    private const int MaxExpiryDays = 90;
    private const int MinExpiryDays = 1;
    private const int MaxUserAgentLength = 500;

    public UserSession()
    { }

    #region Factory Method

    public static UserSession Create(
        int userId,
        string tokenSelector,
        string tokenVerifierHash,
        string ipAddress,
        string? userAgent,
        string sessionType = "Web",
        int expiryDays = DefaultExpiryDays)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        ValidateTokenSelector(tokenSelector);
        ValidateTokenVerifierHash(tokenVerifierHash);
        ValidateIpAddress(ipAddress);
        ValidateExpiryDays(expiryDays);

        return new UserSession
        {
            UserId = userId,
            _tokenSelector = tokenSelector.Trim(),
            _tokenVerifierHash = tokenVerifierHash,
            _expiresAt = DateTime.UtcNow.AddDays(expiryDays),
            _isRevoked = false,
            _createdByIp = ipAddress.Trim(),
            _userAgent = TruncateUserAgent(userAgent),
            _sessionType = sessionType,
            CreatedAt = DateTime.UtcNow,
            _lastActivityAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method for Infrastructure layer (SessionService) that needs to set properties directly
    /// </summary>
    public static UserSession CreateForInfrastructure(
        int userId,
        string tokenSelector,
        string tokenVerifierHash,
        string ipAddress,
        string? userAgent,
        string sessionType = "Web",
        int expiryDays = DefaultExpiryDays)
    {
        return new UserSession
        {
            UserId = userId,
            _tokenSelector = tokenSelector.Trim(),
            _tokenVerifierHash = tokenVerifierHash,
            _expiresAt = DateTime.UtcNow.AddDays(expiryDays),
            _isRevoked = false,
            _createdByIp = ipAddress.Trim(),
            _userAgent = userAgent?.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent,
            _sessionType = sessionType,
            CreatedAt = DateTime.UtcNow,
            _lastActivityAt = DateTime.UtcNow
        };
    }

    #endregion Factory Method

    #region Domain Behaviors

    public bool Verify(string tokenVerifierHash)
    {
        if (!IsActive)
            return false;

        return _tokenVerifierHash == tokenVerifierHash;
    }

    public void Revoke()
    {
        if (_isRevoked) return;

        _isRevoked = true;
        _revokedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordActivity()
    {
        if (!IsActive) return;

        _lastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Refresh(string newTokenVerifierHash, int expiryDays = DefaultExpiryDays)
    {
        if (!IsActive)
            throw new DomainException("امکان تمدید نشست غیرفعال وجود ندارد.");

        ValidateTokenVerifierHash(newTokenVerifierHash);
        ValidateExpiryDays(expiryDays);

        _replacedByTokenHash = _tokenVerifierHash;
        _tokenVerifierHash = newTokenVerifierHash;
        _expiresAt = DateTime.UtcNow.AddDays(expiryDays);
        _lastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExtendExpiry(int additionalDays)
    {
        if (!IsActive)
            throw new DomainException("امکان تمدید نشست غیرفعال وجود ندارد.");

        if (additionalDays <= 0)
            throw new DomainException("تعداد روز باید بزرگتر از صفر باشد.");

        var newExpiryDate = _expiresAt.AddDays(additionalDays);
        var maxExpiryDate = CreatedAt.AddDays(MaxExpiryDays);

        if (newExpiryDate > maxExpiryDate)
            throw new DomainException($"تاریخ انقضا نمی‌تواند بیش از {MaxExpiryDays} روز از تاریخ ایجاد باشد.");

        _expiresAt = newExpiryDate;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Domain Behaviors

    #region Query Methods

    public TimeSpan? GetRemainingTime()
    {
        if (!IsActive) return null;
        return _expiresAt - DateTime.UtcNow;
    }

    public int GetRemainingDays()
    {
        var remaining = GetRemainingTime();
        return remaining.HasValue ? (int)remaining.Value.TotalDays : 0;
    }

    public TimeSpan? GetIdleTime()
    {
        if (!_lastActivityAt.HasValue) return null;
        return DateTime.UtcNow - _lastActivityAt.Value;
    }

    public bool IsIdle(TimeSpan threshold)
    {
        var idleTime = GetIdleTime();
        return idleTime.HasValue && idleTime.Value > threshold;
    }

    public string GetDeviceInfo()
    {
        if (string.IsNullOrEmpty(_userAgent)) return "دستگاه نامشخص";
        var ua = _userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone")) return "موبایل";
        if (ua.Contains("tablet") || ua.Contains("ipad")) return "تبلت";
        return "کامپیوتر";
    }

    public string GetBrowserInfo()
    {
        if (string.IsNullOrEmpty(_userAgent)) return "نامشخص";
        var ua = _userAgent.ToLowerInvariant();
        if (ua.Contains("chrome") && !ua.Contains("edge")) return "Chrome";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("safari") && !ua.Contains("chrome")) return "Safari";
        if (ua.Contains("edge")) return "Edge";
        if (ua.Contains("opera") || ua.Contains("opr")) return "Opera";
        return "نامشخص";
    }

    public string GetSessionSummary()
    {
        return $"{GetDeviceInfo()} - {GetBrowserInfo()} ({_createdByIp})";
    }

    #endregion Query Methods

    #region Static Validation Methods

    private static void ValidateTokenSelector(string tokenSelector)
    {
        if (string.IsNullOrWhiteSpace(tokenSelector))
            throw new DomainException("انتخابگر توکن الزامی است.");
        if (tokenSelector.Length < 16)
            throw new DomainException("انتخابگر توکن باید حداقل ۱۶ کاراکتر باشد.");
    }

    private static void ValidateTokenVerifierHash(string tokenVerifierHash)
    {
        if (string.IsNullOrWhiteSpace(tokenVerifierHash))
            throw new DomainException("هش تأییدکننده توکن الزامی است.");
    }

    private static void ValidateIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("آدرس IP الزامی است.");
    }

    private static void ValidateExpiryDays(int expiryDays)
    {
        if (expiryDays < MinExpiryDays)
            throw new DomainException($"مدت اعتبار باید حداقل {MinExpiryDays} روز باشد.");
        if (expiryDays > MaxExpiryDays)
            throw new DomainException($"مدت اعتبار نمی‌تواند بیش از {MaxExpiryDays} روز باشد.");
    }

    private static string? TruncateUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return null;
        return userAgent.Length > MaxUserAgentLength ? userAgent[..MaxUserAgentLength] : userAgent;
    }

    public void MarkAsReplaced(string newTokenSelector)
    {
        if (string.IsNullOrWhiteSpace(newTokenSelector))
            throw new DomainException("Selector توکن جدید نمی‌تواند خالی باشد.");

        ReplacedByTokenHash = newTokenSelector;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Static Validation Methods
}