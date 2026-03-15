using Domain.User.Entities;
using Domain.User.Events;
using Domain.User.Exceptions;
using Domain.User.ValueObjects;

namespace Domain.User.Aggregates;

public sealed class User : AggregateRoot<UserId>, IAuditable, IActivatable, ISoftDeletable
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);
    private const int MaxAddresses = 10;

    private readonly List<UserAddress> _addresses = new();

    private User()
    { }

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public UserAddressId? DefaultAddressId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyList<UserAddress> Addresses => _addresses.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

    public static User Create(
        UserId id,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("ایمیل الزامی است.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("رمز عبور الزامی است.");

        var user = new User
        {
            Id = id,
            FirstName = firstName?.Trim() ?? string.Empty,
            LastName = lastName?.Trim() ?? string.Empty,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber?.Trim(),
            IsActive = true,
            IsAdmin = false,
            IsEmailVerified = false,
            FailedLoginAttempts = 0,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredEvent(id, email.Trim().ToLowerInvariant(), user.FirstName, user.LastName));
        return user;
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        EnsureNotDeleted();
        EnsureActive();

        FirstName = firstName?.Trim() ?? string.Empty;
        LastName = lastName?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserProfileUpdatedEvent(Id, FirstName, LastName, PhoneNumber));
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("رمز عبور جدید الزامی است.");

        PasswordHash = newPasswordHash;
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserPasswordChangedEvent(Id));
    }

    public void ChangeEmail(string newEmail)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(newEmail))
            throw new DomainException("ایمیل جدید الزامی است.");

        var normalizedEmail = newEmail.Trim().ToLowerInvariant();

        if (string.Equals(Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("ایمیل جدید با ایمیل فعلی یکسان است.");

        Email = normalizedEmail;
        IsEmailVerified = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        EnsureNotDeleted();

        if (IsEmailVerified)
            return;

        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserEmailVerifiedEvent(Id, Email));
    }

    public void Activate()
    {
        EnsureNotDeleted();

        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserActivatedEvent(Id));
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserDeactivatedEvent(Id));
    }

    public void SoftDelete(int? deletedBy = null)
    {
        EnsureNotDeleted();

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserDeletedEvent(Id, deletedBy));
    }

    public void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserRestoredEvent(Id));
    }

    public void PromoteToAdmin()
    {
        EnsureNotDeleted();
        EnsureActive();

        if (IsAdmin)
            return;

        IsAdmin = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserPromotedToAdminEvent(Id));
    }

    public void DemoteFromAdmin()
    {
        EnsureNotDeleted();

        if (!IsAdmin)
            return;

        IsAdmin = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserDemotedFromAdminEvent(Id));
    }

    public void RecordSuccessfulLogin()
    {
        EnsureNotDeleted();
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
        EnsureNotDeleted();

        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            RaiseDomainEvent(new UserLockedOutEvent(Id, LockoutEnd.Value));
            RaiseDomainEvent(new UserLoginFailedEvent(Id, FailedLoginAttempts));
        }
        else
        {
            RaiseDomainEvent(new UserLoginFailedEvent(Id, FailedLoginAttempts));
        }
    }

    public void Unlock()
    {
        EnsureNotDeleted();

        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public UserAddress AddAddress(
        UserAddressId addressId,
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
        EnsureNotDeleted();
        EnsureActive();

        if (_addresses.Count(a => !a.IsDeleted) >= MaxAddresses)
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
        string phoneNumber,
        string province,
        string city,
        string address,
        string postalCode,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        EnsureNotDeleted();
        EnsureActive();

        var userAddress = GetActiveAddress(addressId);
        userAddress.UpdateDetails(title, receiverName, phoneNumber, province, city, address, postalCode, latitude, longitude);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserAddressUpdatedEvent(Id, addressId));
    }

    public void RemoveAddress(UserAddressId addressId)
    {
        EnsureNotDeleted();

        var address = GetActiveAddress(addressId);
        address.Delete();

        if (DefaultAddressId == addressId)
        {
            var newDefault = _addresses.FirstOrDefault(a => !a.IsDeleted && a.Id != addressId);
            DefaultAddressId = newDefault?.Id;
        }

        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserAddressRemovedEvent(Id, addressId));
    }

    public void SetDefaultAddress(UserAddressId addressId)
    {
        EnsureNotDeleted();
        EnsureActive();

        var address = GetActiveAddress(addressId);

        if (DefaultAddressId == addressId)
            return;

        foreach (var existingAddress in _addresses.Where(a => !a.IsDeleted))
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
        if (!DefaultAddressId.HasValue)
            return _addresses.FirstOrDefault(a => !a.IsDeleted);

        return _addresses.FirstOrDefault(a => a.Id == DefaultAddressId && !a.IsDeleted);
    }

    public bool HasAddress(UserAddressId addressId)
        => _addresses.Any(a => a.Id == addressId && !a.IsDeleted);

    public int GetRemainingLoginAttempts()
        => Math.Max(0, MaxFailedLoginAttempts - FailedLoginAttempts);

    private UserAddress GetActiveAddress(UserAddressId addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted);
        if (address is null)
            throw new UserAddressNotFoundException(addressId);
        return address;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("کاربر حذف شده است.");
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("حساب کاربری غیرفعال است.");
    }
}