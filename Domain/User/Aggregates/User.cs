namespace Domain.User.Aggregates;

public sealed class User : AggregateRoot<UserId>
{
    private readonly List<UserAddress> _addresses = new();

    private User()
    { }

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public UserAddressId? DefaultAddressId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<UserAddress> Addresses => _addresses.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}".Trim();

    public static User Create(
        UserId id,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string? phoneNumber = null)
    {
        var user = new User
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber,
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(id, email, firstName, lastName));
        return user;
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserProfileUpdatedEvent(Id, firstName, lastName, phoneNumber));
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void VerifyEmail()
    {
        if (IsEmailVerified)
            return;

        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserEmailVerifiedEvent(Id, Email));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserActivatedEvent(Id));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserDeactivatedEvent(Id));
    }

    public UserAddress AddAddress(
        UserAddressId addressId,
        string fullName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string postalCode,
        string countryCode)
    {
        var address = UserAddress.Create(
            addressId,
            Id,
            fullName,
            phoneNumber,
            addressLine1,
            addressLine2,
            city,
            state,
            postalCode,
            countryCode);

        _addresses.Add(address);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserAddressAddedEvent(Id, addressId));
        return address;
    }

    public void RemoveAddress(UserAddressId addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId)
            ?? throw new UserAddressNotFoundException(addressId);

        _addresses.Remove(address);

        if (DefaultAddressId == addressId)
            DefaultAddressId = _addresses.FirstOrDefault()?.Id;

        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserAddressRemovedEvent(Id, addressId));
    }

    public void SetDefaultAddress(UserAddressId addressId)
    {
        var exists = _addresses.Any(a => a.Id == addressId);

        if (!exists)
            throw new UserAddressNotFoundException(addressId);

        var previous = DefaultAddressId;
        DefaultAddressId = addressId;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserDefaultAddressChangedEvent(Id, previous, addressId));
    }
}