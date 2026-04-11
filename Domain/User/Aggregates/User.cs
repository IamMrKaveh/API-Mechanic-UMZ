using Domain.Security.Events;
using Domain.User.Entities;
using Domain.User.Events;
using Domain.User.Exceptions;
using Domain.User.ValueObjects;

namespace Domain.User.Aggregates;

public sealed class User : AggregateRoot<UserId>, IAuditable, IActivatable
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);
    private const int MaxAddresses = 10;

    private readonly List<UserAddress> _addresses = [];

    private User()
    { }

    public FullName FullName { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public PhoneNumber? PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public UserAddressId? DefaultAddressId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyList<UserAddress> Addresses => _addresses.AsReadOnly();

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public static User Create(
        UserId id,
        FullName fullName,
        Email email,
        string passwordHash,
        PhoneNumber? phoneNumber = null)
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(fullName, nameof(fullName));
        Guard.Against.Null(email, nameof(email));
        Guard.Against.NullOrWhiteSpace(passwordHash, nameof(passwordHash));

        var user = new User
        {
            Id = id,
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber,
            IsActive = true,
            IsAdmin = false,
            IsEmailVerified = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(id, email, fullName.FirstName, fullName.LastName));
        return user;
    }

    public void UpdateProfile(FullName fullName, PhoneNumber? phoneNumber)
    {
        EnsureActive();
        Guard.Against.Null(fullName, nameof(fullName));

        FullName = fullName;
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserProfileUpdatedEvent(Id, FullName.FirstName, FullName.LastName, PhoneNumber?.Value));
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        Guard.Against.NullOrWhiteSpace(newPasswordHash, nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void ChangeEmail(Email newEmail)
    {
        Guard.Against.Null(newEmail, nameof(newEmail));

        if (Email == newEmail)
            throw new DomainException("ایمیل جدید با ایمیل فعلی یکسان است.");

        Email = newEmail;
        IsEmailVerified = false;
        UpdatedAt = DateTime.UtcNow;
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

    public void PromoteToAdmin()
    {
        EnsureActive();

        if (IsAdmin)
            return;

        IsAdmin = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserPromotedToAdminEvent(Id));
    }

    public void DemoteFromAdmin()
    {
        if (!IsAdmin)
            return;

        IsAdmin = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserDemotedFromAdminEvent(Id));
    }

    public void RecordSuccessfulLogin()
    {
        EnsureActive();

        if (IsLockedOut)
            throw new DomainException("حساب کاربری قفل شده است.");

        FailedLoginAttempts = 0;
        LockoutEnd = null;
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserLoggedInEvent(Id));
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            RaiseDomainEvent(new UserLockedOutEvent(Id, LockoutEnd.Value, FailedLoginAttempts));
        }

        RaiseDomainEvent(new UserLoginFailedEvent(Id, FailedLoginAttempts));
    }

    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public UserAddress AddAddress(
        UserAddressId addressId,
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
        EnsureActive();

        if (_addresses.Count >= MaxAddresses)
            throw new DomainException($"حداکثر تعداد آدرس مجاز {MaxAddresses} عدد است.");

        var userAddress = UserAddress.Create(
            addressId,
            Id,
            title,
            receiverName,
            phoneNumber,
            province,
            city,
            address,
            postalCode,
            latitude,
            longitude);

        _addresses.Add(userAddress);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserAddressAddedEvent(Id, addressId));
        return userAddress;
    }

    public void UpdateAddress(
        UserAddressId addressId,
        string title,
        string receiverName,
        PhoneNumber phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        bool isDefault,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        EnsureActive();

        var userAddress = GetAddress(addressId);
        userAddress.UpdateDetails(title, receiverName, phoneNumber, province, city, address, postalCode, latitude, longitude);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserAddressUpdatedEvent(Id, addressId));
    }

    public void RemoveAddress(UserAddressId addressId)
    {
        var address = GetAddress(addressId);
        _addresses.Remove(address);

        if (DefaultAddressId == addressId)
        {
            var newDefault = _addresses.FirstOrDefault();
            DefaultAddressId = newDefault?.Id;
        }

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserAddressRemovedEvent(Id, addressId));
    }

    public void SetDefaultAddress(UserAddressId addressId)
    {
        EnsureActive();

        var address = GetAddress(addressId);

        if (DefaultAddressId == addressId)
            return;

        foreach (var existingAddress in _addresses)
            existingAddress.UnsetDefault();

        address.SetAsDefault();

        var previous = DefaultAddressId;
        DefaultAddressId = addressId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserDefaultAddressChangedEvent(Id, previous, addressId));
        RaiseDomainEvent(new UserAddressSetAsDefaultEvent(Id, addressId));
    }

    public UserAddress? GetDefaultAddress()
    {
        if (Convert.ToString(DefaultAddressId?.Value) != string.Empty)
            return _addresses.FirstOrDefault();

        return _addresses.FirstOrDefault(a => a.Id == DefaultAddressId);
    }

    public bool HasAddress(UserAddressId addressId)
        => _addresses.Any(a => a.Id == addressId);

    public int GetRemainingLoginAttempts()
        => Math.Max(0, MaxFailedLoginAttempts - FailedLoginAttempts);

    private UserAddress GetAddress(UserAddressId addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null)
            throw new UserAddressNotFoundException(addressId);
        return address;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("حساب کاربری غیرفعال است.");
    }

    public void ChangePhoneNumber(PhoneNumber phoneNumber)
    {
        throw new NotImplementedException();
    }
}