using Domain.User.ValueObjects;

namespace Domain.User.Entities;

public sealed class UserAddress : Entity<UserAddressId>, IAuditable, ISoftDeletable
{
    private const int PostalCodeLength = 10;
    private const int MaxTitleLength = 100;
    private const int MaxNameLength = 100;
    private const int MaxAddressLength = 500;
    private const int MaxProvinceLength = 50;
    private const int MaxCityLength = 50;

    private UserAddress()
    { }

    public UserId UserId { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string ReceiverName { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string Province { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    internal static UserAddress Create(
        UserAddressId id,
        UserId userId,
        string title,
        string receiverName,
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        Validate(title, receiverName, phoneNumber, province, city, address, postalCode, latitude, longitude);

        return new UserAddress
        {
            Id = id,
            UserId = userId,
            Title = title.Trim(),
            ReceiverName = receiverName.Trim(),
            PhoneNumber = NormalizePhoneNumber(phoneNumber),
            Province = province.Trim(),
            City = city.Trim(),
            Address = address.Trim(),
            PostalCode = NormalizePostalCode(postalCode),
            Latitude = latitude,
            Longitude = longitude,
            IsDefault = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    internal void UpdateDetails(
        string title,
        string receiverName,
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        Validate(title, receiverName, phoneNumber, province, city, address, postalCode, latitude, longitude);

        Title = title.Trim();
        ReceiverName = receiverName.Trim();
        PhoneNumber = NormalizePhoneNumber(phoneNumber);
        Province = province.Trim();
        City = city.Trim();
        Address = address.Trim();
        PostalCode = NormalizePostalCode(postalCode);
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void UnsetDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void Delete(int? deletedBy = null)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public string GetFullAddress() => $"{Province}، {City}، {Address}";

    public string GetShortAddress() => $"{City}، {Address}";

    public string GetFormattedPostalCode()
    {
        if (PostalCode.Length == PostalCodeLength)
            return $"{PostalCode[..5]}-{PostalCode[5..]}";

        return PostalCode;
    }

    public bool HasLocation() => Latitude.HasValue && Longitude.HasValue;

    private static void Validate(
        string title,
        string receiverName,
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        decimal? latitude,
        decimal? longitude)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان آدرس الزامی است.");

        if (title.Trim().Length > MaxTitleLength)
            throw new DomainException($"عنوان آدرس نمی‌تواند بیش از {MaxTitleLength} کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(receiverName))
            throw new DomainException("نام گیرنده الزامی است.");

        if (receiverName.Trim().Length > MaxNameLength)
            throw new DomainException($"نام گیرنده نمی‌تواند بیش از {MaxNameLength} کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("شماره تلفن الزامی است.");

        ValidatePhoneNumber(phoneNumber);

        if (string.IsNullOrWhiteSpace(province))
            throw new DomainException("استان الزامی است.");

        if (province.Trim().Length > MaxProvinceLength)
            throw new DomainException($"نام استان نمی‌تواند بیش از {MaxProvinceLength} کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("شهر الزامی است.");

        if (city.Trim().Length > MaxCityLength)
            throw new DomainException($"نام شهر نمی‌تواند بیش از {MaxCityLength} کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("آدرس الزامی است.");

        if (address.Trim().Length > MaxAddressLength)
            throw new DomainException($"آدرس نمی‌تواند بیش از {MaxAddressLength} کاراکتر باشد.");

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new DomainException("کد پستی الزامی است.");

        ValidatePostalCode(postalCode);

        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            throw new DomainException("عرض جغرافیایی نامعتبر است.");

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            throw new DomainException("طول جغرافیایی نامعتبر است.");
    }

    private static void ValidatePhoneNumber(string phoneNumber)
    {
        var normalized = NormalizePhoneNumber(phoneNumber);
        if (normalized.Length != 11 || !normalized.StartsWith("09") || !normalized.All(char.IsDigit))
            throw new DomainException("شماره موبایل نامعتبر است.");
    }

    private static void ValidatePostalCode(string postalCode)
    {
        var normalized = NormalizePostalCode(postalCode);
        if (normalized.Length != PostalCodeLength || !normalized.All(char.IsDigit))
            throw new DomainException("کد پستی باید ۱۰ رقم باشد.");
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        digits = digits
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
            .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
            .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8")
            .Replace("۹", "9");

        if (digits.StartsWith("98") && digits.Length == 12)
            digits = string.Concat("0", digits.AsSpan(2));

        if (digits.StartsWith("0098") && digits.Length == 14)
            digits = string.Concat("0", digits.AsSpan(4));

        if (!digits.StartsWith('0') && digits.Length == 10)
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
}