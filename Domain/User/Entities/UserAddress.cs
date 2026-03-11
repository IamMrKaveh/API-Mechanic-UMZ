namespace Domain.User.Entities;

public sealed class UserAddress : Entity<UserAddressId>
{
    private UserAddress()
    { }

    public UserId UserId { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public string AddressLine1 { get; private set; } = default!;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public string CountryCode { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    internal static UserAddress Create(
        UserAddressId id,
        UserId userId,
        string fullName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string postalCode,
        string countryCode)
    {
        return new UserAddress
        {
            Id = id,
            UserId = userId,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            AddressLine1 = addressLine1,
            AddressLine2 = addressLine2,
            City = city,
            State = state,
            PostalCode = postalCode,
            CountryCode = countryCode,
            CreatedAt = DateTime.UtcNow
        };
    }
}