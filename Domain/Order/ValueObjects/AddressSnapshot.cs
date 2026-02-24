namespace Domain.Order.ValueObjects;

public sealed class AddressSnapshot : ValueObject
{
    public int OriginalAddressId { get; }
    public string Title { get; }
    public string ReceiverName { get; }
    public string PhoneNumber { get; }
    public string Province { get; }
    public string City { get; }
    public string Address { get; }
    public string PostalCode { get; }
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }

    private AddressSnapshot(
        int originalAddressId,
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
        OriginalAddressId = originalAddressId;
        Title = title;
        ReceiverName = receiverName;
        PhoneNumber = phoneNumber;
        Province = province;
        City = city;
        Address = address;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
    }

    public static AddressSnapshot Create(
        int originalAddressId,
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
        Guard.Against.NullOrWhiteSpace(receiverName, nameof(receiverName));
        Guard.Against.NullOrWhiteSpace(phoneNumber, nameof(phoneNumber));
        Guard.Against.NullOrWhiteSpace(province, nameof(province));
        Guard.Against.NullOrWhiteSpace(city, nameof(city));
        Guard.Against.NullOrWhiteSpace(address, nameof(address));
        Guard.Against.NullOrWhiteSpace(postalCode, nameof(postalCode));

        ValidatePhoneNumber(phoneNumber);
        ValidatePostalCode(postalCode);

        return new AddressSnapshot(
            originalAddressId,
            title?.Trim() ?? "آدرس",
            receiverName.Trim(),
            phoneNumber.Trim(),
            province.Trim(),
            city.Trim(),
            address.Trim(),
            postalCode.Trim(),
            latitude,
            longitude);
    }

    public static AddressSnapshot FromUserAddress(UserAddress userAddress)
    {
        Guard.Against.Null(userAddress, nameof(userAddress));

        return Create(
            userAddress.Id,
            userAddress.Title,
            userAddress.ReceiverName,
            userAddress.PhoneNumber,
            userAddress.Province,
            userAddress.City,
            userAddress.Address,
            userAddress.PostalCode,
            userAddress.Latitude,
            userAddress.Longitude);
    }

    public string GetFullAddress()
    {
        return $"{Province}، {City}، {Address}";
    }

    public string GetShortAddress()
    {
        return $"{City}، {Address}";
    }

    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            OriginalAddressId,
            Title,
            ReceiverName,
            PhoneNumber,
            Province,
            City,
            Address,
            PostalCode,
            Latitude,
            Longitude
        });
    }

    public static AddressSnapshot FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new DomainException("داده آدرس نامعتبر است.");

        try
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<AddressData>(json);
            if (data == null)
                throw new DomainException("داده آدرس نامعتبر است.");

            return new AddressSnapshot(
                data.OriginalAddressId,
                data.Title ?? "آدرس",
                data.ReceiverName ?? "",
                data.PhoneNumber ?? "",
                data.Province ?? "",
                data.City ?? "",
                data.Address ?? "",
                data.PostalCode ?? "",
                data.Latitude,
                data.Longitude);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new DomainException("فرمت داده آدرس نامعتبر است.");
        }
    }

    private static void ValidatePhoneNumber(string phoneNumber)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^09\d{9}$"))
            throw new DomainException("شماره موبایل نامعتبر است.");
    }

    private static void ValidatePostalCode(string postalCode)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(postalCode, @"^\d{10}$"))
            throw new DomainException("کد پستی باید ۱۰ رقم باشد.");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return OriginalAddressId;
        yield return ReceiverName;
        yield return PhoneNumber;
        yield return Province;
        yield return City;
        yield return Address;
        yield return PostalCode;
    }

    private class AddressData
    {
        public int OriginalAddressId { get; set; }
        public string? Title { get; set; }
        public string? ReceiverName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}