using Domain.User.Events;
using Domain.User.Exceptions;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.User.Entities;

public class UserAddressTests
{
    [Fact]
    public void AddAddress_WithValidInput_ReturnsInitializedUserAddress()
    {
        var user = new UserBuilder().Build();
        var sut = new UserAddressParametersBuilder()
            .WithTitle("خانه")
            .WithReceiverName("علی رضایی")
            .WithProvince("تهران")
            .WithCity("تهران")
            .WithAddress("خیابان ولیعصر، پلاک ۱۲۳")
            .WithPostalCode("1234567890")
            .AddTo(user);

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.UserId.ShouldBe(user.Id);
        sut.Title.ShouldBe("خانه");
        sut.ReceiverName.ShouldBe("علی رضایی");
        sut.Province.ShouldBe("تهران");
        sut.City.ShouldBe("تهران");
        sut.Address.ShouldBe("خیابان ولیعصر، پلاک ۱۲۳");
        sut.PostalCode.ShouldBe("1234567890");
        sut.IsDefault.ShouldBeFalse();
        sut.Latitude.ShouldBeNull();
        sut.Longitude.ShouldBeNull();
    }

    [Fact]
    public void AddAddress_TrimsAllStringInputs()
    {
        var user = new UserBuilder().Build();

        var sut = new UserAddressParametersBuilder()
            .WithTitle("  خانه  ")
            .WithReceiverName("  علی  ")
            .WithProvince("  تهران  ")
            .WithCity("  تهران  ")
            .WithAddress("  آدرس  ")
            .WithPostalCode("  1234567890  ")
            .AddTo(user);

        sut.Title.ShouldBe("خانه");
        sut.ReceiverName.ShouldBe("علی");
        sut.Province.ShouldBe("تهران");
        sut.City.ShouldBe("تهران");
        sut.Address.ShouldBe("آدرس");
        sut.PostalCode.ShouldBe("1234567890");
    }

    [Fact]
    public void AddAddress_SetsCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var user = new UserBuilder().Build();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new UserAddressParametersBuilder().AddTo(user);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AddAddress_WithLatitudeAndLongitude_StoresCoordinates()
    {
        var user = new UserBuilder().Build();

        var sut = new UserAddressParametersBuilder()
            .WithLatitude(35.6892m)
            .WithLongitude(51.3890m)
            .AddTo(user);

        sut.Latitude.ShouldBe(35.6892m);
        sut.Longitude.ShouldBe(51.3890m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAddress_WithNullOrWhitespaceTitle_ThrowsDomainException(string? title)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithTitle(title!).AddTo(user));
    }

    [Fact]
    public void AddAddress_WithTitleOver100Characters_ThrowsDomainException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithTitle(new string('a', 101)).AddTo(user));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAddress_WithNullOrWhitespaceReceiverName_ThrowsDomainException(string? name)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithReceiverName(name!).AddTo(user));
    }

    [Fact]
    public void AddAddress_WithReceiverNameOver100Characters_ThrowsDomainException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithReceiverName(new string('a', 101)).AddTo(user));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAddress_WithNullOrWhitespaceProvince_ThrowsDomainException(string? province)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithProvince(province!).AddTo(user));
    }

    [Fact]
    public void AddAddress_WithProvinceOver50Characters_ThrowsDomainException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithProvince(new string('a', 51)).AddTo(user));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAddress_WithNullOrWhitespaceCity_ThrowsDomainException(string? city)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithCity(city!).AddTo(user));
    }

    [Fact]
    public void AddAddress_WithCityOver50Characters_ThrowsDomainException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithCity(new string('a', 51)).AddTo(user));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAddress_WithNullOrWhitespaceAddress_ThrowsDomainException(string? address)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithAddress(address!).AddTo(user));
    }

    [Fact]
    public void AddAddress_WithAddressOver500Characters_ThrowsDomainException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithAddress(new string('a', 501)).AddTo(user));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAddress_WithNullOrWhitespacePostalCode_ThrowsDomainException(string? postalCode)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithPostalCode(postalCode!).AddTo(user));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("12345678901")]
    public void AddAddress_WithPostalCodeNotTenDigits_ThrowsDomainException(string postalCode)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithPostalCode(postalCode).AddTo(user));
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void AddAddress_WithLatitudeOutOfRange_ThrowsDomainException(decimal latitude)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithLatitude(latitude).AddTo(user));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void AddAddress_WithLongitudeOutOfRange_ThrowsDomainException(decimal longitude)
    {
        var user = new UserBuilder().Build();

        Should.Throw<DomainException>(
            () => new UserAddressParametersBuilder().WithLongitude(longitude).AddTo(user));
    }

    [Fact]
    public void AddAddress_RaisesUserAddressAddedEvent()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();

        var sut = new UserAddressParametersBuilder().AddTo(user);

        var evt = user.DomainEvents.OfType<UserAddressAddedEvent>().Single();
        evt.UserId.ShouldBe(user.Id);
        evt.AddressId.ShouldBe(sut.Id);
    }

    [Fact]
    public void UpdateAddress_WithValidInput_UpdatesFieldsAndBumpsUpdatedAt()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(addressId).AddTo(user);
        user.ClearDomainEvents();

        new UserAddressParametersBuilder()
            .WithAddressId(addressId)
            .WithTitle("محل کار")
            .WithReceiverName("رضا")
            .WithProvince("اصفهان")
            .WithCity("اصفهان")
            .WithAddress("خیابان چهارباغ")
            .WithPostalCode("9876543210")
            .UpdateOn(user);

        var sut = user.Addresses.Single(a => a.Id == addressId);
        sut.Title.ShouldBe("محل کار");
        sut.ReceiverName.ShouldBe("رضا");
        sut.Province.ShouldBe("اصفهان");
        sut.City.ShouldBe("اصفهان");
        sut.Address.ShouldBe("خیابان چهارباغ");
        sut.PostalCode.ShouldBe("9876543210");
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateAddress_RaisesUserAddressUpdatedEvent()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(addressId).AddTo(user);
        user.ClearDomainEvents();

        new UserAddressParametersBuilder().WithAddressId(addressId).WithTitle("جدید").UpdateOn(user);

        var evt = user.DomainEvents.OfType<UserAddressUpdatedEvent>().Single();
        evt.UserId.ShouldBe(user.Id);
        evt.AddressId.ShouldBe(addressId);
    }

    [Fact]
    public void UpdateAddress_WithUnknownAddressId_ThrowsUserAddressNotFoundException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<UserAddressNotFoundException>(
            () => new UserAddressParametersBuilder().WithAddressId(UserAddressId.NewId()).UpdateOn(user));
    }

    [Fact]
    public void SetDefaultAddress_OnExistingAddress_MarksAddressAsDefault()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(addressId).AddTo(user);
        user.ClearDomainEvents();

        user.SetDefaultAddress(addressId);

        var sut = user.Addresses.Single(a => a.Id == addressId);
        sut.IsDefault.ShouldBeTrue();
        user.DefaultAddressId.ShouldBe(addressId);
    }

    [Fact]
    public void SetDefaultAddress_UnsetsPreviousDefaultAddress()
    {
        var user = new UserBuilder().Build();
        var firstId = UserAddressId.NewId();
        var secondId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(firstId).AddTo(user);
        new UserAddressParametersBuilder().WithAddressId(secondId).AddTo(user);
        user.SetDefaultAddress(firstId);

        user.SetDefaultAddress(secondId);

        user.Addresses.Single(a => a.Id == firstId).IsDefault.ShouldBeFalse();
        user.Addresses.Single(a => a.Id == secondId).IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void SetDefaultAddress_RaisesUserDefaultAddressChangedAndUserAddressSetAsDefaultEvents()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(addressId).AddTo(user);
        user.ClearDomainEvents();

        user.SetDefaultAddress(addressId);

        user.DomainEvents.OfType<UserDefaultAddressChangedEvent>().ShouldHaveSingleItem();
        user.DomainEvents.OfType<UserAddressSetAsDefaultEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void SetDefaultAddress_WhenAlreadyDefault_IsNoOp()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(addressId).AddTo(user);
        user.SetDefaultAddress(addressId);
        user.ClearDomainEvents();

        user.SetDefaultAddress(addressId);

        user.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetDefaultAddress_WithUnknownAddressId_ThrowsUserAddressNotFoundException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<UserAddressNotFoundException>(
            () => user.SetDefaultAddress(UserAddressId.NewId()));
    }

    [Fact]
    public void RemoveAddress_ExistingAddress_RemovesFromCollectionAndRaisesEvent()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(addressId).AddTo(user);
        user.ClearDomainEvents();

        user.RemoveAddress(addressId);

        user.Addresses.ShouldBeEmpty();
        user.DomainEvents.OfType<UserAddressRemovedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void RemoveAddress_WhenRemovedWasDefault_ReassignsDefaultToRemainingAddress()
    {
        var user = new UserBuilder().Build();
        var firstId = UserAddressId.NewId();
        var secondId = UserAddressId.NewId();
        new UserAddressParametersBuilder().WithAddressId(firstId).AddTo(user);
        new UserAddressParametersBuilder().WithAddressId(secondId).AddTo(user);
        user.SetDefaultAddress(firstId);

        user.RemoveAddress(firstId);

        user.DefaultAddressId.ShouldBe(secondId);
    }

    [Fact]
    public void RemoveAddress_WithUnknownAddressId_ThrowsUserAddressNotFoundException()
    {
        var user = new UserBuilder().Build();

        Should.Throw<UserAddressNotFoundException>(() => user.RemoveAddress(UserAddressId.NewId()));
    }

    [Fact]
    public void UserAddress_ExposesAuditableContract()
    {
        var user = new UserBuilder().Build();

        var sut = new UserAddressParametersBuilder().AddTo(user);

        sut.ShouldBeAssignableTo<IAuditable>();
    }

    [Fact]
    public void AddAddress_WhenMaxAddressesReached_ThrowsDomainException()
    {
        var user = new UserBuilder().Build();
        for (var i = 0; i < 10; i++)
            new UserAddressParametersBuilder().AddTo(user);

        Should.Throw<DomainException>(() => new UserAddressParametersBuilder().AddTo(user));
    }
}
