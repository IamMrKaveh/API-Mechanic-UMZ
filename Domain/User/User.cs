using System.Security.Cryptography;

namespace Domain.User;

public class User : AggregateRoot, ISoftDeletable, IActivatable, IAuditable
{
    private string _phoneNumber = null!;
    private string? _firstName;
    private string? _lastName;
    private string? _email;
    private bool _isActive = true;
    private bool _isAdmin;
    private int _failedLoginAttempts;
    private DateTime? _lockoutEnd;
    private DateTime? _lastLoginAt;
    private int _loginCount;
    private string? _passwordHash;

    private readonly List<UserAddress> _userAddresses = new();
    private readonly List<UserOtp> _userOtps = new();
    private readonly List<UserSession> _userSessions = new();

    // Public Read-Only Properties
    public string PhoneNumber => _phoneNumber;

    public string? FirstName => _firstName;
    public string? LastName => _lastName;
    public string? Email => _email;
    public bool IsActive => _isActive;
    public bool IsAdmin => _isAdmin;
    public int FailedLoginAttempts => _failedLoginAttempts;
    public DateTime? LockoutEnd => _lockoutEnd;
    public DateTime? LastLoginAt => _lastLoginAt;
    public int LoginCount => _loginCount;
    public string? PasswordHash => _passwordHash;

    // Computed Properties
    public bool IsLockedOut => _lockoutEnd.HasValue && _lockoutEnd.Value > DateTime.UtcNow;

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Collections - Read-Only
    public IReadOnlyCollection<UserAddress> UserAddresses => _userAddresses.AsReadOnly();

    public IReadOnlyCollection<UserOtp> UserOtps => _userOtps.AsReadOnly();
    public IReadOnlyCollection<UserSession> UserSessions => _userSessions.AsReadOnly();

    // Navigation for EF Core — تغییر به public برای دسترسی EF Core Configuration
    public ICollection<ProductReview> Reviews { get; private set; } = new List<ProductReview>();

    public ICollection<DiscountUsage> DiscountUsages { get; private set; } = new List<DiscountUsage>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; private set; } = new List<InventoryTransaction>();
    public ICollection<Notification.Notification> Notifications { get; private set; } = new List<Notification.Notification>();
    public ICollection<Cart.Cart> UserCarts { get; private set; } = new List<Cart.Cart>();
    public ICollection<Order.Order> UserOrders { get; private set; } = new List<Order.Order>();

    // Business Constants
    private const int MaxFailedLoginAttempts = 5;

    private const int LockoutDurationMinutes = 15;
    private const int MaxAddressesPerUser = 10;
    private const int MaxActiveSessionsPerUser = 5;

    private User()
    { }

    #region Factory Methods

    public static User Create(string phoneNumber)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        ValidatePhoneNumber(normalizedPhone);

        var user = new User
        {
            _phoneNumber = normalizedPhone,
            _isActive = true,
            _isAdmin = false,
            CreatedAt = DateTime.UtcNow,
            _failedLoginAttempts = 0,
            _loginCount = 0
        };

        user.AddDomainEvent(new UserCreatedEvent(user.Id, user._phoneNumber));

        return user;
    }

    public static User CreateAdmin(string phoneNumber, string firstName, string lastName)
    {
        ValidateName(firstName, "نام");
        ValidateName(lastName, "نام خانوادگی");

        var user = Create(phoneNumber);
        user._firstName = NormalizePersianText(firstName);
        user._lastName = NormalizePersianText(lastName);
        user._isAdmin = true;

        user.AddDomainEvent(new UserPromotedToAdminEvent(user.Id));

        return user;
    }

    #endregion Factory Methods

    #region Profile Management

    public void UpdateProfile(string? firstName, string? lastName, string? email)
    {
        EnsureCanModify();

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            ValidateName(firstName, "نام");
            _firstName = NormalizePersianText(firstName);
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            ValidateName(lastName, "نام خانوادگی");
            _lastName = NormalizePersianText(lastName);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            ValidateEmail(email);
            _email = email.Trim().ToLowerInvariant();
        }

        TouchUpdatedAt();
        AddDomainEvent(new UserProfileUpdatedEvent(Id));
    }

    public void ChangeEmail(string newEmail)
    {
        EnsureCanModify();
        ValidateEmail(newEmail);

        _email = newEmail.Trim().ToLowerInvariant();
        TouchUpdatedAt();

        AddDomainEvent(new UserProfileUpdatedEvent(Id));
    }

    public void ChangePhoneNumber(string newPhoneNumber)
    {
        EnsureCanModify();

        var normalizedPhone = NormalizePhoneNumber(newPhoneNumber);
        ValidatePhoneNumber(normalizedPhone);

        if (_phoneNumber == normalizedPhone)
            return;

        var oldPhone = _phoneNumber;
        _phoneNumber = normalizedPhone;
        TouchUpdatedAt();

        AddDomainEvent(new UserPhoneChangedEvent(Id, oldPhone, normalizedPhone));
    }

    public string GetFullName()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_firstName))
            parts.Add(_firstName);

        if (!string.IsNullOrWhiteSpace(_lastName))
            parts.Add(_lastName);

        return parts.Any() ? string.Join(" ", parts) : _phoneNumber;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(_firstName) && !string.IsNullOrWhiteSpace(_lastName))
            return $"{_firstName} {_lastName}";

        if (!string.IsNullOrWhiteSpace(_firstName))
            return _firstName;

        if (!string.IsNullOrWhiteSpace(_lastName))
            return _lastName;

        return "کاربر";
    }

    #endregion Profile Management

    #region Address Management

    public UserAddress AddAddress(
        string title,
        string receiverName,
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        bool isDefault = false)
    {
        EnsureCanModify();
        EnsureCanAddMoreAddresses();

        var userAddress = UserAddress.Create(
            Id, title, receiverName, phoneNumber,
            province, city, address, postalCode, isDefault);

        if (isDefault || !_userAddresses.Any(a => !a.IsDeleted))
        {
            ClearDefaultAddresses();
            userAddress.SetAsDefault();
        }

        _userAddresses.Add(userAddress);
        TouchUpdatedAt();

        return userAddress;
    }

    public void UpdateAddress(
        int addressId,
        string title,
        string receiverName,
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        bool isDefault)
    {
        EnsureCanModify();

        var targetAddress = GetAddressOrThrow(addressId);
        targetAddress.Update(title, receiverName, phoneNumber, province, city, address, postalCode, isDefault);

        if (isDefault)
        {
            ClearDefaultAddresses(excludeAddressId: addressId);
        }

        TouchUpdatedAt();
    }

    public void SetDefaultAddress(int addressId)
    {
        EnsureCanModify();

        var targetAddress = GetAddressOrThrow(addressId);

        ClearDefaultAddresses();
        targetAddress.SetAsDefault();
        TouchUpdatedAt();
    }

    public void RemoveAddress(int addressId, int? deletedBy = null)
    {
        EnsureCanModify();

        var address = GetAddressOrThrow(addressId);
        address.Delete(deletedBy);

        if (address.IsDefault)
        {
            var nextDefault = _userAddresses.FirstOrDefault(a => !a.IsDeleted && a.Id != addressId);
            nextDefault?.SetAsDefault();
        }

        TouchUpdatedAt();
    }

    public UserAddress? GetDefaultAddress()
    {
        return _userAddresses.FirstOrDefault(a => !a.IsDeleted && a.IsDefault)
               ?? _userAddresses.FirstOrDefault(a => !a.IsDeleted);
    }

    public IEnumerable<UserAddress> GetActiveAddresses()
    {
        return _userAddresses.Where(a => !a.IsDeleted && a.IsActive);
    }

    #endregion Address Management

    #region OTP Management

    public UserOtp GenerateOtp(string otpHash, int expiryMinutes = 2)
    {
        EnsureCanModify();
        EnsureNotLockedOut();

        InvalidateAllOtps();

        var otp = UserOtp.Create(Id, otpHash, expiryMinutes);
        _userOtps.Add(otp);
        TouchUpdatedAt();

        AddDomainEvent(new OtpGeneratedEvent(Id, _phoneNumber));

        return otp;
    }

    public bool VerifyOtp(string otpCode)
    {
        EnsureNotDeleted();
        EnsureNotLockedOut();

        var otpHash = HashOtp(otpCode);
        var activeOtp = _userOtps
            .Where(o => o.IsValid())
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        if (activeOtp == null)
        {
            RecordFailedLoginAttempt();
            return false;
        }

        var isValid = activeOtp.Verify(otpHash);

        if (!isValid)
        {
            RecordFailedLoginAttempt();
            return false;
        }

        ResetLoginAttempts();
        AddDomainEvent(new OtpVerifiedEvent(Id));

        return true;
    }

    public void InvalidateAllOtps()
    {
        foreach (var otp in _userOtps.Where(o => o.IsValid()))
        {
            otp.Invalidate();
        }
    }

    public (bool CanSend, string? Error, TimeSpan? WaitTime) CheckOtpRateLimit(
        int maxOtpPerHour = 5,
        int minIntervalSeconds = 60)
    {
        var recentOtps = _userOtps
            .Where(o => o.CreatedAt > DateTime.UtcNow.AddHours(-1))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var lastOtp = recentOtps.FirstOrDefault();

        if (lastOtp != null)
        {
            var timeSinceLastOtp = DateTime.UtcNow - lastOtp.CreatedAt;

            if (timeSinceLastOtp.TotalSeconds < minIntervalSeconds)
            {
                var waitTime = TimeSpan.FromSeconds(minIntervalSeconds) - timeSinceLastOtp;
                return (false, $"لطفاً {(int)waitTime.TotalSeconds} ثانیه صبر کنید.", waitTime);
            }
        }

        if (recentOtps.Count >= maxOtpPerHour)
        {
            return (false, "تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً یک ساعت دیگر تلاش کنید.", TimeSpan.FromHours(1));
        }

        return (true, null, null);
    }

    #endregion OTP Management

    #region Session Management

    public UserSession CreateSession(
        string tokenSelector,
        string tokenVerifierHash,
        string ipAddress,
        string? userAgent,
        string sessionType = "Web",
        int expiryDays = 30)
    {
        EnsureCanModify();
        EnsureNotLockedOut();

        EnforceMaxActiveSessions();

        var session = UserSession.Create(
            Id,
            tokenSelector,
            tokenVerifierHash,
            ipAddress,
            userAgent,
            sessionType,
            expiryDays);

        _userSessions.Add(session);
        RecordSuccessfulLogin();

        AddDomainEvent(new SessionCreatedEvent(Id, session.Id, ipAddress));

        return session;
    }

    public void RevokeSession(int sessionId)
    {
        var session = _userSessions.FirstOrDefault(s => s.Id == sessionId);

        if (session == null)
            throw new DomainException("نشست یافت نشد.");

        session.Revoke();
        TouchUpdatedAt();

        AddDomainEvent(new SessionRevokedEvent(Id, sessionId));
    }

    public void RevokeAllSessions()
    {
        foreach (var session in _userSessions.Where(s => s.IsActive))
        {
            session.Revoke();
        }

        TouchUpdatedAt();
        AddDomainEvent(new AllSessionsRevokedEvent(Id));
    }

    public void RevokeAllSessionsExcept(int sessionId)
    {
        foreach (var session in _userSessions.Where(s => s.IsActive && s.Id != sessionId))
        {
            session.Revoke();
        }

        TouchUpdatedAt();
    }

    public UserSession? GetActiveSession(string tokenSelector)
    {
        return _userSessions.FirstOrDefault(s => s.IsActive && s.TokenSelector == tokenSelector);
    }

    public IEnumerable<UserSession> GetActiveSessions()
    {
        return _userSessions.Where(s => s.IsActive);
    }

    public bool ValidateSession(string tokenSelector, string tokenVerifierHash)
    {
        var session = GetActiveSession(tokenSelector);

        if (session == null)
            return false;

        if (!session.Verify(tokenVerifierHash))
            return false;

        session.RecordActivity();
        return true;
    }

    #endregion Session Management

    #region Login & Lockout Management

    public void RecordSuccessfulLogin()
    {
        _failedLoginAttempts = 0;
        _lockoutEnd = null;
        _lastLoginAt = DateTime.UtcNow;
        _loginCount++;
        TouchUpdatedAt();

        AddDomainEvent(new UserLoggedInEvent(Id));
    }

    public void RecordFailedLoginAttempt()
    {
        _failedLoginAttempts++;
        TouchUpdatedAt();

        AddDomainEvent(new UserLoginFailedEvent(Id, _failedLoginAttempts));

        if (_failedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockAccount(TimeSpan.FromMinutes(LockoutDurationMinutes));
        }
    }

    public void LockAccount(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new DomainException("مدت قفل باید بزرگتر از صفر باشد.");

        _lockoutEnd = DateTime.UtcNow.Add(duration);
        TouchUpdatedAt();

        AddDomainEvent(new UserLockedOutEvent(Id, _lockoutEnd.Value));
    }

    public void UnlockAccount()
    {
        if (!IsLockedOut)
            return;

        ResetLoginAttempts();
    }

    public void ResetLoginAttempts()
    {
        _failedLoginAttempts = 0;
        _lockoutEnd = null;
        TouchUpdatedAt();
    }

    public TimeSpan? GetRemainingLockoutTime()
    {
        if (!IsLockedOut)
            return null;

        return _lockoutEnd!.Value - DateTime.UtcNow;
    }

    #endregion Login & Lockout Management

    #region Activation & Deletion

    public void Activate()
    {
        EnsureNotDeleted();

        if (_isActive) return;

        _isActive = true;
        TouchUpdatedAt();

        AddDomainEvent(new UserActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!_isActive) return;

        _isActive = false;
        TouchUpdatedAt();

        RevokeAllSessions();

        AddDomainEvent(new UserDeactivatedEvent(Id));
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        EnsureCanBeDeleted();

        IsDeleted = true;
        _isActive = false;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;

        RevokeAllSessions();
        InvalidateAllOtps();

        AddDomainEvent(new UserDeletedEvent(Id, deletedBy));
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        _isActive = true;
        TouchUpdatedAt();

        AddDomainEvent(new UserRestoredEvent(Id));
    }

    public void PromoteToAdmin()
    {
        EnsureCanModify();

        if (_isAdmin) return;

        _isAdmin = true;
        TouchUpdatedAt();

        AddDomainEvent(new UserPromotedToAdminEvent(Id));
    }

    public void DemoteFromAdmin()
    {
        EnsureCanModify();

        if (!_isAdmin) return;

        _isAdmin = false;
        TouchUpdatedAt();

        AddDomainEvent(new UserDemotedFromAdminEvent(Id));
    }

    #endregion Activation & Deletion

    #region Query Methods

    public bool CanLogin()
    {
        if (IsDeleted) return false;
        if (!_isActive) return false;
        if (IsLockedOut) return false;
        return true;
    }

    public bool CanPlaceOrder()
    {
        if (IsDeleted) return false;
        if (!_isActive) return false;
        return true;
    }

    public bool HasActiveOrders()
    {
        return UserOrders?.Any(o => !o.IsDeleted && !o.IsPaid) ?? false;
    }

    public int GetActiveAddressCount()
    {
        return _userAddresses.Count(a => !a.IsDeleted);
    }

    public int GetActiveSessionCount()
    {
        return _userSessions.Count(s => s.IsActive);
    }

    #endregion Query Methods

    #region Domain Invariants

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("حساب کاربری حذف شده است.");
    }

    private void EnsureActive()
    {
        if (!_isActive)
            throw new DomainException("حساب کاربری غیرفعال است.");
    }

    private void EnsureCanModify()
    {
        EnsureNotDeleted();
        EnsureActive();
    }

    private void EnsureNotLockedOut()
    {
        if (IsLockedOut)
        {
            var remaining = GetRemainingLockoutTime();
            throw new DomainException($"حساب کاربری قفل شده است. لطفاً {(int)remaining!.Value.TotalMinutes} دقیقه دیگر تلاش کنید.");
        }
    }

    private void EnsureCanBeDeleted()
    {
        if (HasActiveOrders())
        {
            throw new DomainException("امکان حذف حساب با سفارش‌های فعال وجود ندارد.");
        }
    }

    private void EnsureCanAddMoreAddresses()
    {
        if (GetActiveAddressCount() >= MaxAddressesPerUser)
        {
            throw new DomainException($"حداکثر تعداد آدرس مجاز {MaxAddressesPerUser} عدد است.");
        }
    }

    private void EnforceMaxActiveSessions()
    {
        var activeSessions = _userSessions.Where(s => s.IsActive).OrderBy(s => s.LastActivityAt).ToList();

        while (activeSessions.Count >= MaxActiveSessionsPerUser)
        {
            var oldestSession = activeSessions.First();
            oldestSession.Revoke();
            activeSessions.Remove(oldestSession);
        }
    }

    private UserAddress GetAddressOrThrow(int addressId)
    {
        var address = _userAddresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted);

        if (address == null)
            throw new DomainException("آدرس یافت نشد.");

        return address;
    }

    private void ClearDefaultAddresses(int? excludeAddressId = null)
    {
        foreach (var addr in _userAddresses.Where(a => a.IsDefault && (!excludeAddressId.HasValue || a.Id != excludeAddressId.Value)))
        {
            addr.SetAsNonDefault();
        }
    }

    public void ChangePassword(string currentPasswordHash, string newPasswordHash)
    {
        EnsureCanModify();

        if (!string.IsNullOrEmpty(_passwordHash) && _passwordHash != currentPasswordHash)
            throw new DomainException("رمز عبور فعلی نادرست است.");

        _passwordHash = newPasswordHash;
        TouchUpdatedAt();
    }

    private void TouchUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Domain Invariants

    #region Static Validation Methods

    private static void ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("شماره تلفن الزامی است.");

        if (phoneNumber.Length != 11)
            throw new InvalidPhoneNumberException(phoneNumber);

        if (!phoneNumber.StartsWith("09"))
            throw new InvalidPhoneNumberException(phoneNumber);

        if (!phoneNumber.All(char.IsDigit))
            throw new InvalidPhoneNumberException(phoneNumber);
    }

    private static void ValidateName(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (name.Trim().Length > 50)
            throw new DomainException($"{fieldName} نباید بیش از ۵۰ کاراکتر باشد.");

        if (!IsPersianOrEnglish(name))
            throw new DomainException($"{fieldName} فقط می‌تواند شامل حروف فارسی یا انگلیسی باشد.");
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("آدرس ایمیل الزامی است.");

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email.Trim())
                throw new DomainException("آدرس ایمیل نامعتبر است.");
        }
        catch (FormatException)
        {
            throw new DomainException("آدرس ایمیل نامعتبر است.");
        }
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var normalized = phoneNumber.Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("+98", "0")
            .Replace("0098", "0");

        normalized = normalized
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
            .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
            .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8")
            .Replace("۹", "9");

        if (!normalized.StartsWith("0") && normalized.Length == 10)
        {
            normalized = "0" + normalized;
        }

        return normalized;
    }

    private static string NormalizePersianText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("ى", "ی");
    }

    private static bool IsPersianOrEnglish(string text)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(text.Trim(), @"^[\u0600-\u06FFa-zA-Z\s]+$");
    }

    private static string GenerateOtpCode(int length = 6)
    {
        Span<char> buffer = stackalloc char[6];
        Span<char> digits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
        int available = 10;

        Span<byte> rnd = stackalloc byte[1];

        for (int i = 0; i < length; i++)
        {
            RandomNumberGenerator.Fill(rnd);
            int index = rnd[0] % available;
            buffer[i] = digits[index];
            digits[index] = digits[available - 1];
            available--;
        }

        return new string(buffer);
    }

    private static string HashOtp(string otp)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(otp);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public void ResetLockout()
    {
        _failedLoginAttempts = 0;
        _lockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetIsActive(bool isActive)
    {
        _isActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateName(string firstName, string lastName)
    {
        _firstName = firstName?.Trim();
        _lastName = lastName?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAdminStatus(bool isAdmin)
    {
        _isAdmin = isAdmin;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Static Validation Methods
}