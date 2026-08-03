using Domain.Security.Events;
using Domain.User.Events;
using Domain.User.Exceptions;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Domain.User.Aggregates;

public class UserTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedUser()
    {
        var fullName = new FullNameBuilder().WithFirstName("John").WithLastName("Doe").Build();
        var email = Email.Create("user@example.com");
        var phone = new PhoneNumberBuilder().WithValue("09121234567").Build();

        var sut = new UserBuilder()
            .WithFullName(fullName)
            .WithEmail(email)
            .WithPasswordHash("hash")
            .WithPhoneNumber(phone)
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.FullName.ShouldBe(fullName);
        sut.Email.ShouldBe(email);
        sut.PhoneNumber.ShouldBe(phone);
        sut.PasswordHash.ShouldBe("hash");
        sut.IsActive.ShouldBeTrue();
        sut.IsAdmin.ShouldBeFalse();
        sut.IsEmailVerified.ShouldBeFalse();
        sut.FailedLoginAttempts.ShouldBe(0);
        sut.LockoutEnd.ShouldBeNull();
        sut.LastLoginAt.ShouldBeNull();
        sut.DefaultAddressId.ShouldBeNull();
        sut.Addresses.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new UserBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldNotBeNull();
        sut.UpdatedAt.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Create_ProducesUserWithVersionOne()
    {
        new UserBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneUserRegisteredEvent()
    {
        var sut = new UserBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<UserRegisteredEvent>();
    }

    [Fact]
    public void Create_WithNullFullName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => new UserBuilder().WithFullName(null!).Build());
    }

    [Fact]
    public void Create_WithNullEmail_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => new UserBuilder().WithEmail((Email)null!).Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespacePasswordHash_ThrowsArgumentException(string? passwordHash)
    {
        Should.Throw<ArgumentException>(
            () => new UserBuilder().WithPasswordHash(passwordHash!).Build());
    }

    [Fact]
    public void Create_WithoutPhoneNumber_LeavesPhoneNumberNull()
    {
        var sut = new UserBuilder().WithPhoneNumber(null).Build();

        sut.PhoneNumber.ShouldBeNull();
    }

    [Fact]
    public void Create_IsAssignableToActivatableAndAuditable()
    {
        var sut = new UserBuilder().Build();

        sut.ShouldBeAssignableTo<IActivatable>();
        sut.ShouldBeAssignableTo<IAuditable>();
    }

    [Fact]
    public void RegisterByPhone_WithValidPhone_ReturnsUserWithTempEmailAndEmptyPasswordHash()
    {
        var phone = new PhoneNumberBuilder().WithValue("09121234567").Build();

        var sut = Users.RegisterByPhone(phone);

        sut.PhoneNumber.ShouldBe(phone);
        sut.PasswordHash.ShouldBe(string.Empty);
        sut.Email.Value.ShouldEndWith("@temp.local");
        sut.FullName.FirstName.ShouldBe(string.Empty);
        sut.FullName.LastName.ShouldBe(string.Empty);
        sut.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void RegisterByPhone_WithNullPhone_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Users.RegisterByPhone(null!));
    }

    [Fact]
    public void RegisterByPhone_RaisesUserRegisteredEventWithEmptyNames()
    {
        var phone = new PhoneNumberBuilder().WithValue("09121234567").Build();

        var sut = Users.RegisterByPhone(phone);

        var evt = sut.DomainEvents.Single().ShouldBeOfType<UserRegisteredEvent>();
        evt.FirstName.ShouldBe(string.Empty);
        evt.LastName.ShouldBe(string.Empty);
    }

    [Fact]
    public void UpdateProfile_OnActiveUser_AppliesFullNameAndPhoneAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();
        var newFullName = new FullNameBuilder().WithFirstName("Jane").WithLastName("Smith").Build();
        var newPhone = new PhoneNumberBuilder().WithValue("09189999999").Build();

        sut.UpdateProfile(newFullName, newPhone);

        sut.FullName.ShouldBe(newFullName);
        sut.PhoneNumber.ShouldBe(newPhone);
        sut.DomainEvents.Single().ShouldBeOfType<UserProfileUpdatedEvent>();
    }

    [Fact]
    public void UpdateProfile_OnInactiveUser_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();

        Should.Throw<DomainException>(
            () => sut.UpdateProfile(new FullNameBuilder().Build(), null));
    }

    [Fact]
    public void UpdateProfile_WithNullFullName_ThrowsArgumentException()
    {
        var sut = new UserBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.UpdateProfile(null!, null));
    }

    [Fact]
    public void UpdateProfile_WithNullPhoneNumber_ClearsExistingPhone()
    {
        var sut = new UserBuilder().WithPhoneNumber(new PhoneNumberBuilder().Build()).Build();

        sut.UpdateProfile(new FullNameBuilder().Build(), null);

        sut.PhoneNumber.ShouldBeNull();
    }

    [Fact]
    public void ChangePasswordHash_AppliesNewHashAndResetsLoginState()
    {
        var sut = new UserBuilder().Build();
        sut.RecordFailedLogin();
        sut.RecordFailedLogin();
        sut.ClearDomainEvents();

        sut.ChangePasswordHash("new-hash");

        sut.PasswordHash.ShouldBe("new-hash");
        sut.FailedLoginAttempts.ShouldBe(0);
        sut.LockoutEnd.ShouldBeNull();
        sut.DomainEvents.Single().ShouldBeOfType<UserPasswordChangedEvent>();
    }

    [Fact]
    public void ChangePasswordHash_ClearsLockoutOnLockedOutUser()
    {
        var sut = new UserBuilder().Build();
        for (var i = 0; i < 5; i++) sut.RecordFailedLogin();

        sut.IsLockedOut.ShouldBeTrue();
        sut.ChangePasswordHash("new-hash");

        sut.IsLockedOut.ShouldBeFalse();
        sut.LockoutEnd.ShouldBeNull();
        sut.FailedLoginAttempts.ShouldBe(0);
    }

    [Fact]
    public void ChangePasswordHash_DoesNotEnforceActiveGate()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();

        Should.NotThrow(() => sut.ChangePasswordHash("new-hash"));
        sut.PasswordHash.ShouldBe("new-hash");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangePasswordHash_WithNullOrWhitespace_ThrowsArgumentException(string? input)
    {
        var sut = new UserBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.ChangePasswordHash(input!));
    }

    [Fact]
    public void VerifyEmail_WhenNotVerified_SetsFlagAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();

        sut.VerifyEmail();

        sut.IsEmailVerified.ShouldBeTrue();
        sut.DomainEvents.Single().ShouldBeOfType<UserEmailVerifiedEvent>();
    }

    [Fact]
    public void VerifyEmail_WhenAlreadyVerified_IsNoOp()
    {
        var sut = new UserBuilder().Build();
        sut.VerifyEmail();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.VerifyEmail();

        sut.DomainEvents.ShouldBeEmpty();
        sut.Version.ShouldBe(versionBefore);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOp()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.DomainEvents.ShouldBeEmpty();
        sut.Version.ShouldBe(versionBefore);
    }

    [Fact]
    public void Activate_OnDeactivatedUser_SetsIsActiveTrueAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.DomainEvents.Single().ShouldBeOfType<UserActivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveFalseAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.DomainEvents.Single().ShouldBeOfType<UserDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();

        sut.Deactivate();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void PromoteToAdmin_WhenNotAdmin_SetsFlagAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();

        sut.PromoteToAdmin();

        sut.IsAdmin.ShouldBeTrue();
        sut.DomainEvents.Single().ShouldBeOfType<UserPromotedToAdminEvent>();
    }

    [Fact]
    public void PromoteToAdmin_WhenAlreadyAdmin_IsNoOp()
    {
        var sut = new UserBuilder().Build();
        sut.PromoteToAdmin();
        sut.ClearDomainEvents();

        sut.PromoteToAdmin();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void PromoteToAdmin_OnInactiveUser_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();

        Should.Throw<DomainException>(sut.PromoteToAdmin);
    }

    [Fact]
    public void DemoteFromAdmin_WhenAdmin_SetsFlagFalseAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.PromoteToAdmin();
        sut.ClearDomainEvents();

        sut.DemoteFromAdmin();

        sut.IsAdmin.ShouldBeFalse();
        sut.DomainEvents.Single().ShouldBeOfType<UserDemotedFromAdminEvent>();
    }

    [Fact]
    public void DemoteFromAdmin_WhenNotAdmin_IsNoOp()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();

        sut.DemoteFromAdmin();

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void DemoteFromAdmin_DoesNotEnforceActiveGate()
    {
        var sut = new UserBuilder().Build();
        sut.PromoteToAdmin();
        sut.Deactivate();
        sut.ClearDomainEvents();

        Should.NotThrow(sut.DemoteFromAdmin);
        sut.IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void RecordSuccessfulLogin_ResetsFailedAttemptsAndSetsLastLoginAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.RecordFailedLogin();
        sut.RecordFailedLogin();
        sut.ClearDomainEvents();

        sut.RecordSuccessfulLogin();

        sut.FailedLoginAttempts.ShouldBe(0);
        sut.LastLoginAt.ShouldNotBeNull();
        sut.DomainEvents.Single().ShouldBeOfType<UserLoggedInEvent>();
    }

    [Fact]
    public void RecordSuccessfulLogin_OnInactiveUser_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();

        Should.Throw<DomainException>(sut.RecordSuccessfulLogin);
    }

    [Fact]
    public void RecordSuccessfulLogin_OnLockedOutUser_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        for (var i = 0; i < 5; i++) sut.RecordFailedLogin();

        Should.Throw<DomainException>(sut.RecordSuccessfulLogin);
    }

    [Fact]
    public void RecordFailedLogin_BelowThreshold_IncrementsCounterAndRaisesOnlyFailedEvent()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();

        sut.RecordFailedLogin();

        sut.FailedLoginAttempts.ShouldBe(1);
        sut.IsLockedOut.ShouldBeFalse();
        sut.DomainEvents.Single().ShouldBeOfType<UserLoginFailedEvent>();
    }

    [Fact]
    public void RecordFailedLogin_AtThreshold_LocksOutAndRaisesTwoEvents()
    {
        var sut = new UserBuilder().Build();
        for (var i = 0; i < 4; i++) sut.RecordFailedLogin();
        sut.ClearDomainEvents();

        sut.RecordFailedLogin();

        sut.FailedLoginAttempts.ShouldBe(5);
        sut.IsLockedOut.ShouldBeTrue();
        sut.LockoutEnd.ShouldNotBeNull();
        sut.LockoutEnd.Value.ShouldBeGreaterThan(DateTime.UtcNow);
        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<UserLockedOutEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<UserLoginFailedEvent>();
    }

    [Fact]
    public void RecordFailedLogin_DoesNotEnforceActiveGate()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();

        Should.NotThrow(sut.RecordFailedLogin);
        sut.FailedLoginAttempts.ShouldBe(1);
    }

    [Fact]
    public void IsLockedOut_WhenLockoutEndNull_ReturnsFalse()
    {
        new UserBuilder().Build().IsLockedOut.ShouldBeFalse();
    }

    [Fact]
    public void GetRemainingLoginAttempts_ReturnsFiveMinusFailedAttempts()
    {
        var sut = new UserBuilder().Build();

        sut.GetRemainingLoginAttempts().ShouldBe(5);
        sut.RecordFailedLogin();
        sut.GetRemainingLoginAttempts().ShouldBe(4);
        sut.RecordFailedLogin();
        sut.RecordFailedLogin();
        sut.GetRemainingLoginAttempts().ShouldBe(2);
    }

    [Fact]
    public void GetRemainingLoginAttempts_NeverReturnsNegative()
    {
        var sut = new UserBuilder().Build();
        for (var i = 0; i < 5; i++) sut.RecordFailedLogin();

        sut.GetRemainingLoginAttempts().ShouldBe(0);
    }

    [Fact]
    public void AddAddress_WithValidInput_AddsAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        sut.ClearDomainEvents();

        var address = new UserAddressParametersBuilder().AddTo(sut);

        sut.Addresses.Count.ShouldBe(1);
        sut.Addresses.Single().ShouldBe(address);
        sut.DomainEvents.Single().ShouldBeOfType<UserAddressAddedEvent>();
    }

    [Fact]
    public void AddAddress_TrimsTextualFields()
    {
        var sut = new UserBuilder().Build();

        var address = new UserAddressParametersBuilder()
            .WithTitle("  خانه  ")
            .WithReceiverName("  علی رضایی  ")
            .WithProvince("  تهران  ")
            .WithCity("  تهران  ")
            .WithAddress("  خیابان ولیعصر  ")
            .WithPostalCode("  1234567890  ")
            .AddTo(sut);

        address.Title.ShouldBe("خانه");
        address.ReceiverName.ShouldBe("علی رضایی");
        address.Province.ShouldBe("تهران");
        address.City.ShouldBe("تهران");
        address.Address.ShouldBe("خیابان ولیعصر");
        address.PostalCode.ShouldBe("1234567890");
    }

    [Fact]
    public void AddAddress_OnInactiveUser_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        sut.Deactivate();

        Should.Throw<DomainException>(() => new UserAddressParametersBuilder().AddTo(sut));
    }

    [Fact]
    public void AddAddress_At10MaxLimit_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        for (var i = 0; i < 10; i++)
            new UserAddressParametersBuilder().AddTo(sut);

        Should.Throw<DomainException>(() => new UserAddressParametersBuilder().AddTo(sut));
        sut.Addresses.Count.ShouldBe(10);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAddress_WithEmptyTitle_ThrowsDomainException(string title)
    {
        var sut = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithTitle(title).AddTo(sut));
    }

    [Fact]
    public void AddAddress_WithInvalidPostalCodeLength_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithPostalCode("123").AddTo(sut));
    }

    [Fact]
    public void AddAddress_WithLatitudeOutOfRange_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithLatitude(91m).AddTo(sut));
    }

    [Fact]
    public void AddAddress_WithLongitudeOutOfRange_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithLongitude(-181m).AddTo(sut));
    }

    [Fact]
    public void UpdateAddress_WithExistingId_AppliesChangesAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        var paramsBuilder = new UserAddressParametersBuilder();
        var address = paramsBuilder.AddTo(sut);
        sut.ClearDomainEvents();

        paramsBuilder.WithTitle("محل کار").UpdateOn(sut);

        address.Title.ShouldBe("محل کار");
        sut.DomainEvents.Single().ShouldBeOfType<UserAddressUpdatedEvent>();
    }

    [Fact]
    public void UpdateAddress_WithUnknownId_ThrowsUserAddressNotFoundException()
    {
        var sut = new UserBuilder().Build();

        Should.Throw<UserAddressNotFoundException>(
            () => new UserAddressParametersBuilder()
                .WithAddressId(UserAddressId.NewId())
                .UpdateOn(sut));
    }

    [Fact]
    public void UpdateAddress_OnInactiveUser_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        var paramsBuilder = new UserAddressParametersBuilder();
        paramsBuilder.AddTo(sut);
        sut.Deactivate();

        Should.Throw<DomainException>(() => paramsBuilder.UpdateOn(sut));
    }

    [Fact]
    public void RemoveAddress_WithExistingId_RemovesAndRaisesEvent()
    {
        var sut = new UserBuilder().Build();
        var paramsBuilder = new UserAddressParametersBuilder();
        var address = paramsBuilder.AddTo(sut);
        sut.ClearDomainEvents();

        sut.RemoveAddress(address.Id);

        sut.Addresses.ShouldBeEmpty();
        sut.DomainEvents.Single().ShouldBeOfType<UserAddressRemovedEvent>();
    }

    [Fact]
    public void RemoveAddress_WithUnknownId_ThrowsUserAddressNotFoundException()
    {
        var sut = new UserBuilder().Build();

        Should.Throw<UserAddressNotFoundException>(
            () => sut.RemoveAddress(UserAddressId.NewId()));
    }

    [Fact]
    public void RemoveAddress_DoesNotEnforceActiveGate()
    {
        var sut = new UserBuilder().Build();
        var paramsBuilder = new UserAddressParametersBuilder();
        var address = paramsBuilder.AddTo(sut);
        sut.Deactivate();

        Should.NotThrow(() => sut.RemoveAddress(address.Id));
        sut.Addresses.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveAddress_WhenRemovingDefault_PromotesFirstRemainingAddressToDefault()
    {
        var sut = new UserBuilder().Build();
        var firstParams = new UserAddressParametersBuilder();
        var secondParams = new UserAddressParametersBuilder();
        var first = firstParams.AddTo(sut);
        var second = secondParams.AddTo(sut);
        sut.SetDefaultAddress(first.Id);

        sut.RemoveAddress(first.Id);

        sut.DefaultAddressId.ShouldBe(second.Id);
    }

    [Fact]
    public void RemoveAddress_WhenRemovingLastDefault_LeavesDefaultAddressIdNull()
    {
        var sut = new UserBuilder().Build();
        var paramsBuilder = new UserAddressParametersBuilder();
        var address = paramsBuilder.AddTo(sut);
        sut.SetDefaultAddress(address.Id);

        sut.RemoveAddress(address.Id);

        sut.DefaultAddressId.ShouldBeNull();
        sut.Addresses.ShouldBeEmpty();
    }

    [Fact]
    public void SetDefaultAddress_WithExistingId_UpdatesDefaultAndRaisesTwoEvents()
    {
        var sut = new UserBuilder().Build();
        var address = new UserAddressParametersBuilder().AddTo(sut);
        sut.ClearDomainEvents();

        sut.SetDefaultAddress(address.Id);

        sut.DefaultAddressId.ShouldBe(address.Id);
        address.IsDefault.ShouldBeTrue();
        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<UserDefaultAddressChangedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<UserAddressSetAsDefaultEvent>();
    }

    [Fact]
    public void SetDefaultAddress_WithSameIdAlreadyDefault_IsNoOp()
    {
        var sut = new UserBuilder().Build();
        var address = new UserAddressParametersBuilder().AddTo(sut);
        sut.SetDefaultAddress(address.Id);
        sut.ClearDomainEvents();

        sut.SetDefaultAddress(address.Id);

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetDefaultAddress_UnsetsPreviousDefault()
    {
        var sut = new UserBuilder().Build();
        var firstAddr = new UserAddressParametersBuilder().AddTo(sut);
        var secondAddr = new UserAddressParametersBuilder().AddTo(sut);
        sut.SetDefaultAddress(firstAddr.Id);

        sut.SetDefaultAddress(secondAddr.Id);

        firstAddr.IsDefault.ShouldBeFalse();
        secondAddr.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void SetDefaultAddress_OnInactiveUser_ThrowsDomainException()
    {
        var sut = new UserBuilder().Build();
        var address = new UserAddressParametersBuilder().AddTo(sut);
        sut.Deactivate();

        Should.Throw<DomainException>(() => sut.SetDefaultAddress(address.Id));
    }

    [Fact]
    public void SetDefaultAddress_WithUnknownId_ThrowsUserAddressNotFoundException()
    {
        var sut = new UserBuilder().Build();

        Should.Throw<UserAddressNotFoundException>(
            () => sut.SetDefaultAddress(UserAddressId.NewId()));
    }

    [Fact]
    public void HasAddress_WithExistingId_ReturnsTrue()
    {
        var sut = new UserBuilder().Build();
        var address = new UserAddressParametersBuilder().AddTo(sut);

        sut.HasAddress(address.Id).ShouldBeTrue();
    }

    [Fact]
    public void HasAddress_WithUnknownId_ReturnsFalse()
    {
        new UserBuilder().Build().HasAddress(UserAddressId.NewId()).ShouldBeFalse();
    }

    [Fact]
    public void ChangePhoneNumber_WhenNoPreviousPhone_ThrowsDomainException()
    {
        var sut = new UserBuilder().WithPhoneNumber(null).Build();
        var newPhone = new PhoneNumberBuilder().Build();

        Should.Throw<DomainException>(() => sut.ChangePhoneNumber(newPhone));
    }

    [Fact]
    public void ChangePhoneNumber_WithNull_ThrowsArgumentException()
    {
        var sut = new UserBuilder().WithPhoneNumber(new PhoneNumberBuilder().Build()).Build();

        Should.Throw<ArgumentException>(() => sut.ChangePhoneNumber(null!));
    }

    [Fact]
    public void ChangePhoneNumber_WithSamePhone_IsNoOp()
    {
        var phone = new PhoneNumberBuilder().WithValue("09121234567").Build();
        var sut = new UserBuilder().WithPhoneNumber(phone).Build();
        sut.ClearDomainEvents();

        sut.ChangePhoneNumber(PhoneNumber.Create("09121234567"));

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangePhoneNumber_WithDifferentPhoneOnActive_AppliesAndRaisesEvent()
    {
        var oldPhone = new PhoneNumberBuilder().WithValue("09121111111").Build();
        var newPhone = new PhoneNumberBuilder().WithValue("09122222222").Build();
        var sut = new UserBuilder().WithPhoneNumber(oldPhone).Build();
        sut.ClearDomainEvents();

        sut.ChangePhoneNumber(newPhone);

        sut.PhoneNumber.ShouldBe(newPhone);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<UserPhoneChangedEvent>();
        evt.OldPhone.ShouldBe(oldPhone);
        evt.NewPhone.ShouldBe(newPhone);
    }

    [Fact]
    public void ChangePhoneNumber_WithDifferentPhoneOnInactive_ThrowsDomainException()
    {
        var oldPhone = new PhoneNumberBuilder().WithValue("09121111111").Build();
        var newPhone = new PhoneNumberBuilder().WithValue("09122222222").Build();
        var sut = new UserBuilder().WithPhoneNumber(oldPhone).Build();
        sut.Deactivate();

        Should.Throw<DomainException>(() => sut.ChangePhoneNumber(newPhone));
    }

    [Fact]
    public void LifecycleSequence_VersionGrowsByEventCount()
    {
        var sut = new UserBuilder().WithPhoneNumber(new PhoneNumberBuilder().WithValue("09121111111").Build()).Build();

        sut.Version.ShouldBe(1);
        sut.VerifyEmail();
        sut.Version.ShouldBe(2);
        sut.PromoteToAdmin();
        sut.Version.ShouldBe(3);
        sut.DemoteFromAdmin();
        sut.Version.ShouldBe(4);
        sut.Deactivate();
        sut.Version.ShouldBe(5);
        sut.Activate();
        sut.Version.ShouldBe(6);
    }

    [Fact]
    public void Equality_TwoUsersWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new UserBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoUsersWithDifferentIds_AreConsideredUnequal()
    {
        var a = new UserBuilder().Build();
        var b = new UserBuilder().Build();

        a.Equals(b).ShouldBeFalse();
    }
}
