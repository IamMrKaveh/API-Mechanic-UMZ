using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.User.Aggregates;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutAddressResolverServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly CheckoutAddressResolverService _sut;

    public CheckoutAddressResolverServiceTests()
    {
        _sut = new CheckoutAddressResolverService(_userRepository);
    }

    private static global::Domain.User.Aggregates.User NewUserWithAddress(out UserAddressId addressId)
    {
        var user = new UserBuilder().Build();
        var parameters = new UserAddressParametersBuilder()
            .WithTitle("Home")
            .WithReceiverName("Ali Rezaei")
            .WithPhoneNumber(PhoneNumber.Create("09121234567"))
            .WithProvince("Tehran")
            .WithCity("Tehran")
            .WithAddress("Valiasr St 123")
            .WithPostalCode("1234567890");
        var address = parameters.AddTo(user);
        addressId = address.Id;
        user.ClearDomainEvents();
        return user;
    }

    [Fact]
    public async Task ResolveAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.User.Aggregates.User?)null);

        var result = await _sut.ResolveAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ResolveAsync_WhenAddressDoesNotBelongToUser_ReturnsNotFound()
    {
        var user = NewUserWithAddress(out _);
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.ResolveAsync(user.Id.Value, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ResolveAsync_WhenAddressExists_MapsReceiverInfoAndDeliveryAddress()
    {
        var user = NewUserWithAddress(out var addressId);
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.ResolveAsync(user.Id.Value, addressId.Value, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ReceiverInfo.FullName.ShouldBe("Ali Rezaei");
        result.Value.ReceiverInfo.PhoneNumber.ShouldBe("09121234567");
        result.Value.DeliveryAddress.Province.ShouldBe("Tehran");
        result.Value.DeliveryAddress.City.ShouldBe("Tehran");
        result.Value.DeliveryAddress.Street.ShouldBe("Valiasr St 123");
        result.Value.DeliveryAddress.PostalCode.ShouldBe("1234567890");
    }

    [Fact]
    public async Task ResolveAsync_ForwardsUserIdAddressIdAndCancellationToken()
    {
        var user = NewUserWithAddress(out var addressId);
        var ct = new CancellationTokenSource().Token;
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await _sut.ResolveAsync(user.Id.Value, addressId.Value, ct);

        await _userRepository.Received(1).GetWithAddressesAsync(
            Arg.Is<UserId>(id => id == user.Id), ct);
    }

    [Fact]
    public async Task ResolveAsync_WhenUserHasMultipleAddresses_ResolvesRequestedOne()
    {
        var user = new UserBuilder().Build();
        var home = new UserAddressParametersBuilder()
            .WithTitle("Home")
            .WithCity("Tehran")
            .AddTo(user);
        var office = new UserAddressParametersBuilder()
            .WithTitle("Office")
            .WithCity("Karaj")
            .AddTo(user);
        user.ClearDomainEvents();
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.ResolveAsync(user.Id.Value, office.Id.Value, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.DeliveryAddress.City.ShouldBe("Karaj");
        home.Id.ShouldNotBe(office.Id);
    }
}
