namespace Domain.User.Services;

/// <summary>
/// Domain Service برای عملیات‌های پیچیده کاربر که بین چند Aggregate هستند
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public sealed class UserDomainService
{
    /// <summary>
    /// اعتبارسنجی امکان لاگین کاربر
    /// </summary>
    public UserLoginValidation ValidateLogin(User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return UserLoginValidation.Failed("حساب کاربری حذف شده است.");

        if (!user.IsActive)
            return UserLoginValidation.Failed("حساب کاربری غیرفعال است.");

        if (user.IsLockedOut)
        {
            var remaining = user.GetRemainingLockoutTime();
            return UserLoginValidation.LockedOut(remaining!.Value);
        }

        return UserLoginValidation.Success();
    }

    /// <summary>
    /// اعتبارسنجی امکان تغییر اطلاعات کاربر
    /// </summary>
    public (bool CanModify, string? Error) ValidateCanModify(User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return (false, "حساب کاربری حذف شده است.");

        if (!user.IsActive)
            return (false, "حساب کاربری غیرفعال است.");

        return (true, null);
    }

    /// <summary>
    /// اعتبارسنجی امکان حذف کاربر
    /// </summary>
    public (bool CanDelete, string? Error) ValidateCanDelete(User user, bool hasActiveOrders)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return (false, "حساب کاربری قبلاً حذف شده است.");

        if (hasActiveOrders)
            return (false, "امکان حذف حساب با سفارش‌های فعال وجود ندارد.");

        return (true, null);
    }

    /// <summary>
    /// اعتبارسنجی امکان ثبت سفارش
    /// </summary>
    public (bool CanOrder, string? Error) ValidateCanPlaceOrder(User user)
    {
        Guard.Against.Null(user, nameof(user));

        if (user.IsDeleted)
            return (false, "حساب کاربری حذف شده است.");

        if (!user.IsActive)
            return (false, "حساب کاربری غیرفعال است.");

        if (!user.GetActiveAddresses().Any())
            return (false, "لطفاً ابتدا یک آدرس ثبت کنید.");

        return (true, null);
    }

    /// <summary>
    /// محاسبه آمار کاربر
    /// </summary>
    public UserStatistics CalculateStatistics(User user)
    {
        Guard.Against.Null(user, nameof(user));

        var activeSessions = user.GetActiveSessionCount();
        var activeAddresses = user.GetActiveAddressCount();

        var accountAge = DateTime.UtcNow - user.CreatedAt;

        return new UserStatistics(
            user.LoginCount,
            activeSessions,
            activeAddresses,
            user.LastLoginAt,
            (int)accountAge.TotalDays,
            user.IsAdmin);
    }

    /// <summary>
    /// اعتبارسنجی و نرمال‌سازی شماره تلفن
    /// </summary>
    public PhoneNumberValidation ValidateAndNormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return PhoneNumberValidation.Invalid("شماره تلفن الزامی است.");

        var normalized = NormalizePhoneNumber(phoneNumber);

        if (!IsValidPhoneNumber(normalized))
            return PhoneNumberValidation.Invalid("شماره تلفن باید با ۰۹ شروع شود و ۱۱ رقم باشد.");

        return PhoneNumberValidation.Valid(normalized);
    }

    /// <summary>
    /// اعتبارسنجی نام و نام خانوادگی
    /// </summary>
    public (bool IsValid, List<string> Errors) ValidateName(string? firstName, string? lastName)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            if (firstName.Length > 50)
                errors.Add("نام نباید بیش از ۵۰ کاراکتر باشد.");

            if (!IsPersianOrEnglish(firstName))
                errors.Add("نام فقط می‌تواند شامل حروف فارسی یا انگلیسی باشد.");
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            if (lastName.Length > 50)
                errors.Add("نام خانوادگی نباید بیش از ۵۰ کاراکتر باشد.");

            if (!IsPersianOrEnglish(lastName))
                errors.Add("نام خانوادگی فقط می‌تواند شامل حروف فارسی یا انگلیسی باشد.");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// اعتبارسنجی آدرس
    /// </summary>
    public AddressValidation ValidateAddress(
        string? title,
        string? receiverName,
        string? phoneNumber,
        string? province,
        string? city,
        string? address,
        string? postalCode)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(receiverName))
            errors.Add("نام گیرنده الزامی است.");
        else if (receiverName.Length > 100)
            errors.Add("نام گیرنده نباید بیش از ۱۰۰ کاراکتر باشد.");

        var phoneValidation = ValidateAndNormalizePhoneNumber(phoneNumber ?? "");
        if (!phoneValidation.IsValid)
            errors.Add(phoneValidation.Error!);

        if (string.IsNullOrWhiteSpace(province))
            errors.Add("استان الزامی است.");

        if (string.IsNullOrWhiteSpace(city))
            errors.Add("شهر الزامی است.");

        if (string.IsNullOrWhiteSpace(address))
            errors.Add("آدرس الزامی است.");
        else if (address.Length > 500)
            errors.Add("آدرس نباید بیش از ۵۰۰ کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(postalCode))
            errors.Add("کد پستی الزامی است.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(postalCode.Trim(), @"^\d{10}$"))
            errors.Add("کد پستی باید ۱۰ رقم باشد.");

        return new AddressValidation(errors.Count == 0, errors);
    }

    /// <summary>
    /// پاکسازی سشن‌های غیرفعال کاربر
    /// </summary>
    public int CleanupInactiveSessions(User user, TimeSpan idleThreshold)
    {
        Guard.Against.Null(user, nameof(user));

        var revokedCount = 0;

        foreach (var session in user.UserSessions.Where(s => s.IsActive))
        {
            if (session.IsIdle(idleThreshold))
            {
                session.Revoke();
                revokedCount++;
            }
        }

        return revokedCount;
    }

    /// <summary>
    /// تعیین سطح امنیت حساب کاربر
    /// </summary>
    public AccountSecurityLevel DetermineSecurityLevel(User user)
    {
        Guard.Against.Null(user, nameof(user));

        var score = 0;

        
        if (!string.IsNullOrWhiteSpace(user.Email))
            score += 20;

        
        if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
            score += 15;

        
        if (user.GetActiveAddressCount() > 0)
            score += 15;

        
        if (user.LoginCount >= 10)
            score += 20;
        else if (user.LoginCount >= 5)
            score += 10;

        
        if (user.FailedLoginAttempts == 0)
            score += 15;

        
        var accountAge = DateTime.UtcNow - user.CreatedAt;
        if (accountAge.TotalDays >= 365)
            score += 15;
        else if (accountAge.TotalDays >= 90)
            score += 10;

        return score switch
        {
            >= 80 => AccountSecurityLevel.High,
            >= 50 => AccountSecurityLevel.Medium,
            _ => AccountSecurityLevel.Low
        };
    }

    #region Private Helper Methods

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        
        digits = digits
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
            .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
            .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8")
            .Replace("۹", "9");

        
        if (digits.StartsWith("98") && digits.Length == 12)
            digits = "0" + digits.Substring(2);

        
        if (!digits.StartsWith("0") && digits.Length == 10)
            digits = "0" + digits;

        return digits;
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        if (phoneNumber.Length != 11)
            return false;

        if (!phoneNumber.StartsWith("09"))
            return false;

        return phoneNumber.All(char.IsDigit);
    }

    private static bool IsPersianOrEnglish(string text)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(text.Trim(), @"^[\u0600-\u06FFa-zA-Z\s]+$");
    }

    #endregion
}

#region Result Types

/// <summary>
/// نتیجه اعتبارسنجی لاگین
/// </summary>
public sealed class UserLoginValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public bool IsLockedOut { get; private set; }
    public TimeSpan? RemainingLockoutTime { get; private set; }

    private UserLoginValidation() { }

    public static UserLoginValidation Success()
    {
        return new UserLoginValidation { IsValid = true };
    }

    public static UserLoginValidation Failed(string error)
    {
        return new UserLoginValidation { IsValid = false, Error = error };
    }

    public static UserLoginValidation LockedOut(TimeSpan remainingTime)
    {
        return new UserLoginValidation
        {
            IsValid = false,
            IsLockedOut = true,
            RemainingLockoutTime = remainingTime,
            Error = $"حساب کاربری قفل شده است. لطفاً {(int)remainingTime.TotalMinutes} دقیقه دیگر تلاش کنید."
        };
    }
}

/// <summary>
/// نتیجه اعتبارسنجی شماره تلفن
/// </summary>
public sealed class PhoneNumberValidation
{
    public bool IsValid { get; private set; }
    public string? NormalizedPhoneNumber { get; private set; }
    public string? Error { get; private set; }

    private PhoneNumberValidation() { }

    public static PhoneNumberValidation Valid(string normalizedPhoneNumber)
    {
        return new PhoneNumberValidation
        {
            IsValid = true,
            NormalizedPhoneNumber = normalizedPhoneNumber
        };
    }

    public static PhoneNumberValidation Invalid(string error)
    {
        return new PhoneNumberValidation
        {
            IsValid = false,
            Error = error
        };
    }
}

/// <summary>
/// نتیجه اعتبارسنجی آدرس
/// </summary>
public sealed class AddressValidation
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    public AddressValidation(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
    }

    public string GetErrorsSummary() => string.Join(" ", Errors);
}

/// <summary>
/// آمار کاربر
/// </summary>
public sealed record UserStatistics(
    int TotalLogins,
    int ActiveSessions,
    int ActiveAddresses,
    DateTime? LastLoginAt,
    int AccountAgeDays,
    bool IsAdmin)
{
    public bool HasRecentActivity =>
        LastLoginAt.HasValue && (DateTime.UtcNow - LastLoginAt.Value).TotalDays < 30;

    public string GetAccountAgeDisplay()
    {
        if (AccountAgeDays < 30)
            return $"{AccountAgeDays} روز";

        if (AccountAgeDays < 365)
            return $"{AccountAgeDays / 30} ماه";

        return $"{AccountAgeDays / 365} سال";
    }
}

/// <summary>
/// سطح امنیت حساب
/// </summary>
public enum AccountSecurityLevel
{
    Low,
    Medium,
    High
}

#endregion