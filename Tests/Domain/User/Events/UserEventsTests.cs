using Domain.User.Events;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.Domain.User.Events;

public class UserEventsTests
{
    [Fact]
    public void UserRegisteredEvent_ExposesConstructorArgumentsAsProperties()
    {
        var userId = UserId.NewId();
        var email = Email.Create("user@example.com");

        var sut = new UserRegisteredEvent(userId, email, "John", "Doe");

        sut.UserId.ShouldBe(userId);
        sut.Email.ShouldBe(email);
        sut.FirstName.ShouldBe("John");
        sut.LastName.ShouldBe("Doe");
    }

    [Fact]
    public void UserProfileUpdatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var userId = UserId.NewId();

        var sut = new UserProfileUpdatedEvent(userId, "John", "Doe", "09121234567");

        sut.UserId.ShouldBe(userId);
        sut.FirstName.ShouldBe("John");
        sut.LastName.ShouldBe("Doe");
        sut.PhoneNumber.ShouldBe("09121234567");
    }

    [Fact]
    public void UserProfileUpdatedEvent_WithNullPhoneNumber_StoresNull()
    {
        var sut = new UserProfileUpdatedEvent(UserId.NewId(), "John", "Doe", null);

        sut.PhoneNumber.ShouldBeNull();
    }

    [Fact]
    public void UserPasswordChangedEvent_ExposesUserId()
    {
        var userId = UserId.NewId();

        new UserPasswordChangedEvent(userId).UserId.ShouldBe(userId);
    }

    [Fact]
    public void UserEmailVerifiedEvent_ExposesUserIdAndEmail()
    {
        var userId = UserId.NewId();
        var email = Email.Create("user@example.com");

        var sut = new UserEmailVerifiedEvent(userId, email);

        sut.UserId.ShouldBe(userId);
        sut.Email.ShouldBe(email);
    }

    [Fact]
    public void UserActivatedEvent_ExposesUserId()
    {
        var userId = UserId.NewId();

        new UserActivatedEvent(userId).UserId.ShouldBe(userId);
    }

    [Fact]
    public void UserDeactivatedEvent_ExposesUserId()
    {
        var userId = UserId.NewId();

        new UserDeactivatedEvent(userId).UserId.ShouldBe(userId);
    }

    [Fact]
    public void UserPromotedToAdminEvent_ExposesUserId()
    {
        var userId = UserId.NewId();

        new UserPromotedToAdminEvent(userId).UserId.ShouldBe(userId);
    }

    [Fact]
    public void UserDemotedFromAdminEvent_ExposesUserId()
    {
        var userId = UserId.NewId();

        new UserDemotedFromAdminEvent(userId).UserId.ShouldBe(userId);
    }

    [Fact]
    public void UserPhoneChangedEvent_ExposesUserIdAndOldAndNewPhones()
    {
        var userId = UserId.NewId();
        var oldPhone = PhoneNumber.Create("09121111111");
        var newPhone = PhoneNumber.Create("09122222222");

        var sut = new UserPhoneChangedEvent(userId, oldPhone, newPhone);

        sut.UserId.ShouldBe(userId);
        sut.OldPhone.ShouldBe(oldPhone);
        sut.NewPhone.ShouldBe(newPhone);
    }

    [Fact]
    public void UserAddressAddedEvent_ExposesUserIdAndAddressId()
    {
        var userId = UserId.NewId();
        var addressId = UserAddressId.NewId();

        var sut = new UserAddressAddedEvent(userId, addressId);

        sut.UserId.ShouldBe(userId);
        sut.AddressId.ShouldBe(addressId);
    }

    [Fact]
    public void UserAddressUpdatedEvent_ExposesUserIdAndAddressId()
    {
        var userId = UserId.NewId();
        var addressId = UserAddressId.NewId();

        var sut = new UserAddressUpdatedEvent(userId, addressId);

        sut.UserId.ShouldBe(userId);
        sut.AddressId.ShouldBe(addressId);
    }

    [Fact]
    public void UserAddressRemovedEvent_ExposesUserIdAndAddressId()
    {
        var userId = UserId.NewId();
        var addressId = UserAddressId.NewId();

        var sut = new UserAddressRemovedEvent(userId, addressId);

        sut.UserId.ShouldBe(userId);
        sut.AddressId.ShouldBe(addressId);
    }

    [Fact]
    public void UserAddressSetAsDefaultEvent_ExposesUserIdAndAddressId()
    {
        var userId = UserId.NewId();
        var addressId = UserAddressId.NewId();

        var sut = new UserAddressSetAsDefaultEvent(userId, addressId);

        sut.UserId.ShouldBe(userId);
        sut.AddressId.ShouldBe(addressId);
    }

    [Fact]
    public void UserDefaultAddressChangedEvent_ExposesPreviousAndNewAddressIds()
    {
        var userId = UserId.NewId();
        var previous = UserAddressId.NewId();
        var next = UserAddressId.NewId();

        var sut = new UserDefaultAddressChangedEvent(userId, previous, next);

        sut.UserId.ShouldBe(userId);
        sut.PreviousDefaultAddressId.ShouldBe(previous);
        sut.NewDefaultAddressId.ShouldBe(next);
    }

    [Fact]
    public void UserDefaultAddressChangedEvent_WithNullPreviousAddressId_StoresNull()
    {
        var sut = new UserDefaultAddressChangedEvent(UserId.NewId(), null, UserAddressId.NewId());

        sut.PreviousDefaultAddressId.ShouldBeNull();
    }
}
