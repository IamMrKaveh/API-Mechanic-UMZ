using Application.Common.Interfaces;
using Application.User.Features.Commands.DeleteUserAddress;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.DeleteUserAddress;

public class DeleteUserAddressHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly DeleteUserAddressHandler _sut;

    public DeleteUserAddressHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new DeleteUserAddressHandler(_userRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(new DeleteUserAddressCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenAddressDoesNotExist_ReturnsFailureAndDoesNotUpdate()
    {
        var user = new UserBuilder().Build();

        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new DeleteUserAddressCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenAddressExists_RemovesAddressAndUpdatesRepository()
    {
        var user = new UserBuilder().Build();
        var addressId = UserAddressId.NewId();
        user.AddAddress(
            addressId,
            "Home",
            "Ali Ahmadi",
            PhoneNumber.Create("09121234567"),
            "Tehran",
            "Tehran",
            "Some street 12",
            "1234567890");

        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new DeleteUserAddressCommand(addressId.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        user.Addresses.ShouldBeEmpty();
        _userRepository.Received(1).Update(user);
    }
}
