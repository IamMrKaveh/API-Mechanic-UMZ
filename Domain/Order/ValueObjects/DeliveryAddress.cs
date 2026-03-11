namespace Domain.Order.ValueObjects;

public sealed record DeliveryAddress
{
    public string Province { get; }
    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private DeliveryAddress(string province, string city, string street, string postalCode)
    {
        Province = province;
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    public static DeliveryAddress Create(string province, string city, string street, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(province))
            throw new ArgumentException("Province cannot be empty.", nameof(province));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty.", nameof(city));
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street cannot be empty.", nameof(street));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be empty.", nameof(postalCode));

        return new DeliveryAddress(
            province.Trim(),
            city.Trim(),
            street.Trim(),
            postalCode.Trim());
    }

    public override string ToString() => $"{Province}, {City}, {Street} - {PostalCode}";
}