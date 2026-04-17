using Domain.User.ValueObjects;

namespace Domain.User.Entities;

public sealed class UserAddress : Entity<UserAddressId>, IAuditable
{
    private const int PostalCodeLength = 10;
    private const int MaxTitleLength = 100;
    private const int MaxNameLength = 100;
    private const int MaxAddressLength = 500;
    private const int MaxProvinceLength = 50;
    private const int MaxCityLength = 50;

    private UserAddress()
    { }

    public User.Aggregates.User User { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string ReceiverName { get; private set; } = default!;
    public PhoneNumber PhoneNumber { get; private set; } = default!;
    public string Province { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    internal static UserAddress Create(
        UserAddressId id,
        UserId userId,
        string title,
        string receiverName,
        PhoneNumber phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        Validate(title, receiverName, province, city, address, postalCode, latitude, longitude);

        return new UserAddress
        {
            Id = id,
            UserId = userId,
            Title = title.Trim(),
            ReceiverName = receiverName.Trim(),
            PhoneNumber = phoneNumber,
            Province = province.Trim(),
            City = city.Trim(),
            Address = address.Trim(),
            PostalCode = postalCode.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    internal void UpdateDetails(
        string title,
        string receiverName,
        PhoneNumber phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        Validate(title, receiverName, province, city, address, postalCode, latitude, longitude);

        Title = title.Trim();
        ReceiverName = receiverName.Trim();
        PhoneNumber = phoneNumber;
        Province = province.Trim();
        City = city.Trim();
        Address = address.Trim();
        PostalCode = postalCode.Trim();
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

    public string GetFullAddress() => $"{Province}، {City}، {Address}";

    public string GetShortAddress() => $"{City}، {Address}";

    public bool HasLocation() => Latitude.HasValue && Longitude.HasValue;

    private static void Validate(
        string title,
        string receiverName,
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
            throw new DomainException($"عنوان آدرس نمی‌تواند بیش از {MaxTitleLength} ��اراکتر باشد.");

        if (string.IsNullOrWhiteSpace(receiverName))
            throw new DomainException("نام گیرنده الزامی است.");

        if (receiverName.Trim().Length > MaxNameLength)
            throw new DomainException($"نام گیرنده نمی‌تواند بیش از {MaxNameLength} کاراکتر باشد.");

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

        if (postalCode.Trim().Length != PostalCodeLength)
            throw new DomainException("کد پستی باید ۱۰ رقم باشد.");

        if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            throw new DomainException("عرض جغرافیایی نامعتبر است.");

        if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            throw new DomainException("طول جغرافیایی نامعتبر است.");
    }
}