using Application.Common.Interfaces;
using Application.User.Features.Commands.UpdateUserAddress;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.UpdateUserAddress;

public class UpdateUserAddressHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly UpdateUserAddressHandler _sut;

    public UpdateUserAddressHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new UpdateUserAddressHandler(_userRepository, _currentUserService);
    }

    private static UpdateUserAddressCommand BuildCommand(Guid addressId, bool isDefault = false) =>
        new(
            AddressId: addressId,
            Title: "Updated",
            ReceiverName: "Ali Ahmadi",
            PhoneNumber: "09121234567",
            Province: "Tehran",
            City: "Tehran",
            Address: "New street 22",
            PostalCode: "1234567890",
            IsDefault: isDefault,
            Latitude: null,
            Longitude: null);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(BuildCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenAddressExists_UpdatesAddressAndCallsRepositoryUpdate()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        user.AddAddress(
            addressId,
            "Home",
            "Ali Ahmadi",
            PhoneNumber.Create("09120000000"),
            "Tehran",
            "Tehran",
            "Old street 1",
            "1111111111");

        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(BuildCommand(addressId.Value, isDefault: true), CancellationToken.None);

        result.ShouldBeSuccess();
        var address = user.Addresses.ShouldHaveSingleItem();
        address.Title.ShouldBe("Updated");
        address.Address.ShouldBe("New street 22");
        address.PostalCode.ShouldBe("1234567890");
        address.IsDefault.ShouldBeTrue();
        _userRepository.Received(1).Update(user);
    }
}
