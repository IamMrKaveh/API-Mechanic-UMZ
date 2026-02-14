namespace Domain.User.ValueObjects;

public sealed class Address : ValueObject
{
    public string Province { get; }
    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }

    private const int MaxProvinceLength = 50;
    private const int MaxCityLength = 50;
    private const int MaxStreetLength = 500;
    private const int PostalCodeLength = 10;

    private Address(string province, string city, string street, string postalCode, decimal? latitude, decimal? longitude)
    {
        Province = province;
        City = city;
        Street = street;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Address Create(
        string province,
        string city,
        string street,
        string postalCode,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        ValidateProvince(province);
        ValidateCity(city);
        ValidateStreet(street);
        ValidatePostalCode(postalCode);

        if (latitude.HasValue)
            ValidateLatitude(latitude.Value);

        if (longitude.HasValue)
            ValidateLongitude(longitude.Value);

        return new Address(
            NormalizePersian(province),
            NormalizePersian(city),
            street.Trim(),
            NormalizePostalCode(postalCode),
            latitude,
            longitude);
    }

    public string GetFullAddress()
    {
        return $"{Province}، {City}، {Street}";
    }

    public string GetShortAddress()
    {
        return $"{City}، {Street}";
    }

    public string GetFormattedPostalCode()
    {
        if (PostalCode.Length == 10)
            return $"{PostalCode.Substring(0, 5)}-{PostalCode.Substring(5)}";

        return PostalCode;
    }

    public bool HasLocation() => Latitude.HasValue && Longitude.HasValue;

    public Address WithLocation(decimal latitude, decimal longitude)
    {
        ValidateLatitude(latitude);
        ValidateLongitude(longitude);

        return new Address(Province, City, Street, PostalCode, latitude, longitude);
    }

    public Address WithoutLocation()
    {
        return new Address(Province, City, Street, PostalCode, null, null);
    }

    #region Validation Methods

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

    private static void ValidateStreet(string street)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("آدرس الزامی است.");

        if (street.Trim().Length > MaxStreetLength)
            throw new DomainException($"آدرس نمی‌تواند بیش از {MaxStreetLength} کاراکتر باشد.");
    }

    private static void ValidatePostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new DomainException("کد پستی الزامی است.");

        var normalized = NormalizePostalCode(postalCode);

        if (normalized.Length != PostalCodeLength || !normalized.All(char.IsDigit))
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

    private static string NormalizePersian(string text)
    {
        return text.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("ى", "ی");
    }

    private static string NormalizePostalCode(string postalCode)
    {
        return new string(postalCode.Where(char.IsDigit).ToArray())
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
            .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
            .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8")
            .Replace("۹", "9");
    }

    #endregion Validation Methods

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Province.ToLowerInvariant();
        yield return City.ToLowerInvariant();
        yield return Street.ToLowerInvariant();
        yield return PostalCode;
    }

    public override string ToString() => GetFullAddress();
}