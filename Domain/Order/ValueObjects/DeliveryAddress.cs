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
            throw new DomainException("Province cannot be empty.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City cannot be empty.");
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("Street cannot be empty.");
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new DomainException("Postal code cannot be empty.");

        return new DeliveryAddress(
            province.Trim(),
            city.Trim(),
            street.Trim(),
            postalCode.Trim());
    }

    public override string ToString() => $"{Province}, {City}, {Street} - {PostalCode}";
}