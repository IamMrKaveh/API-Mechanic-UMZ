namespace Domain.User;

public class UserAddress : BaseEntity, IAuditable, ISoftDeletable, IActivatable
{
    private string _title = null!;
    private string _receiverName = null!;
    private string _phoneNumber = null!;
    private string _province = null!;
    private string _city = null!;
    private string _address = null!;
    private string _postalCode = null!;
    private decimal? _latitude;
    private decimal? _longitude;
    private bool _isDefault;
    private bool _isActive = true;

    public int UserId { get; private set; }
    public string Title => _title;
    public string ReceiverName => _receiverName;
    public string PhoneNumber => _phoneNumber;
    public string Province => _province;
    public string City => _city;
    public string Address => _address;
    public string PostalCode => _postalCode;
    public decimal? Latitude => _latitude;
    public decimal? Longitude => _longitude;
    public bool IsDefault => _isDefault;
    public bool IsActive => _isActive;

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation
    public User? User { get; private set; }

    // Business Constants
    private const int MaxTitleLength = 100;

    private const int MaxReceiverNameLength = 100;
    private const int MaxProvinceLength = 50;
    private const int MaxCityLength = 50;
    private const int MaxAddressLength = 500;
    private const int PostalCodeLength = 10;

    private UserAddress()
    { }

    #region Factory Method

    public static UserAddress Create(
        int userId,
        string title,
        string receiverName,
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        bool isDefault = false)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        ValidateTitle(title);
        ValidateReceiverName(receiverName);
        ValidatePhoneNumber(phoneNumber);
        ValidateProvince(province);
        ValidateCity(city);
        ValidateAddress(address);
        ValidatePostalCode(postalCode);

        return new UserAddress
        {
            UserId = userId,
            _title = title.Trim(),
            _receiverName = NormalizePersianText(receiverName),
            _phoneNumber = NormalizePhoneNumber(phoneNumber),
            _province = NormalizePersianText(province),
            _city = NormalizePersianText(city),
            _address = address.Trim(),
            _postalCode = NormalizePostalCode(postalCode),
            _isDefault = isDefault,
            _isActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion Factory Method

    #region Update Methods

    internal void Update(
        string title,
        string receiverName,
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        bool isDefault)
    {
        EnsureNotDeleted();

        ValidateTitle(title);
        ValidateReceiverName(receiverName);
        ValidatePhoneNumber(phoneNumber);
        ValidateProvince(province);
        ValidateCity(city);
        ValidateAddress(address);
        ValidatePostalCode(postalCode);

        _title = title.Trim();
        _receiverName = NormalizePersianText(receiverName);
        _phoneNumber = NormalizePhoneNumber(phoneNumber);
        _province = NormalizePersianText(province);
        _city = NormalizePersianText(city);
        _address = address.Trim();
        _postalCode = NormalizePostalCode(postalCode);
        _isDefault = isDefault;

        TouchUpdatedAt();
    }

    public void SetLocation(decimal latitude, decimal longitude)
    {
        EnsureNotDeleted();

        ValidateLatitude(latitude);
        ValidateLongitude(longitude);

        _latitude = latitude;
        _longitude = longitude;
        TouchUpdatedAt();
    }

    public void ClearLocation()
    {
        EnsureNotDeleted();

        _latitude = null;
        _longitude = null;
        TouchUpdatedAt();
    }

    internal void SetAsDefault()
    {
        EnsureNotDeleted();

        _isDefault = true;
        TouchUpdatedAt();
    }

    internal void SetAsNonDefault()
    {
        _isDefault = false;
        TouchUpdatedAt();
    }

    public void Activate()
    {
        EnsureNotDeleted();

        if (_isActive) return;

        _isActive = true;
        TouchUpdatedAt();
    }

    public void Deactivate()
    {
        if (!_isActive) return;

        _isActive = false;
        _isDefault = false;
        TouchUpdatedAt();
    }

    internal void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        _isDefault = false;
        _isActive = false;
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        _isActive = true;
        TouchUpdatedAt();
    }

    #endregion Update Methods

    #region Query Methods

    public string GetFullAddress()
    {
        return $"{_province}، {_city}، {_address}";
    }

    public string GetShortAddress()
    {
        return $"{_city}، {_address}";
    }

    public bool HasLocation()
    {
        return _latitude.HasValue && _longitude.HasValue;
    }

    public string GetFormattedPostalCode()
    {
        if (_postalCode.Length == 10)
        {
            return $"{_postalCode.Substring(0, 5)}-{_postalCode.Substring(5)}";
        }
        return _postalCode;
    }

    public string GetMaskedPhoneNumber()
    {
        if (_phoneNumber.Length < 7)
            return _phoneNumber;

        return $"{_phoneNumber.Substring(0, 4)}***{_phoneNumber.Substring(_phoneNumber.Length - 4)}";
    }

    #endregion Query Methods

    #region Private Methods

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("آدرس حذف شده است.");
    }

    private void TouchUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Private Methods

    #region Static Validation Methods

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان آدرس الزامی است.");

        if (title.Trim().Length > MaxTitleLength)
            throw new DomainException($"عنوان آدرس نمی‌تواند بیش از {MaxTitleLength} کاراکتر باشد.");
    }

    private static void ValidateReceiverName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام گیرنده الزامی است.");

        if (name.Trim().Length > MaxReceiverNameLength)
            throw new DomainException($"نام گیرنده نمی‌تواند بیش از {MaxReceiverNameLength} کاراکتر باشد.");
    }

    private static void ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("شماره تلفن الزامی است.");

        var normalized = NormalizePhoneNumber(phoneNumber);

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^09\d{9}$"))
            throw new DomainException("فرمت شماره تلفن نامعتبر است.");
    }

    private static void ValidateProvince(string province)
    {
        if (string.IsNullOrWhiteSpace(province))
            throw new DomainException("استان الزامی است.");

        if (province.Trim().Length > MaxProvinceLength)
            throw new DomainException($"نام استان نمی‌تواند بیش از {MaxProvinceLength} کاراکتر باشد.");
    }

    private static void ValidateCity(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("شهر الزامی است.");

        if (city.Trim().Length > MaxCityLength)
            throw new DomainException($"نام شهر نمی‌تواند بیش از {MaxCityLength} کاراکتر باشد.");
    }

    private static void ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("آدرس الزامی است.");

        if (address.Trim().Length > MaxAddressLength)
            throw new DomainException($"آدرس نمی‌تواند بیش از {MaxAddressLength} کاراکتر باشد.");
    }

    private static void ValidatePostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new DomainException("کد پستی الزامی است.");

        var normalized = NormalizePostalCode(postalCode);

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^\d{10}$"))
            throw new DomainException("کد پستی باید ۱۰ رقم باشد.");
    }

    private static void ValidateLatitude(decimal latitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new DomainException("عرض جغرافیایی نامعتبر است.");
    }

    private static void ValidateLongitude(decimal longitude)
    {
        if (longitude < -180 || longitude > 180)
            throw new DomainException("طول جغرافیایی نامعتبر است.");
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray())
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

    private static string NormalizePostalCode(string postalCode)
    {
        return new string(postalCode.Where(char.IsDigit).ToArray())
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
            .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
            .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8")
            .Replace("۹", "9");
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

    #endregion Static Validation Methods
}