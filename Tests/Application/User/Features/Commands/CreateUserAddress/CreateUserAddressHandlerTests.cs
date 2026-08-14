using Application.Common.Interfaces;
using Application.User.Features.Commands.CreateUserAddress;
using Application.User.Features.Shared;
using Domain.User.Entities;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.CreateUserAddress;

public class CreateUserAddressHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly CreateUserAddressHandler _sut;

    public CreateUserAddressHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new CreateUserAddressHandler(_userRepository, _currentUserService, _mapper);
    }

    private static CreateUserAddressCommand BuildCommand(bool isDefault = false) =>
        new(
            Title: "Home",
            ReceiverName: "Ali Ahmadi",
            PhoneNumber: "09121234567",
            Province: "Tehran",
            City: "Tehran",
            Address: "Some street 1",
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

        var result = await _sut.Handle(BuildCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUserFound_AddsAddressAndReturnsMappedDto()
    {
        var user = new UserBuilder().Build();
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var expectedDto = new UserAddressDto { Id = Guid.NewGuid(), Title = "Home" };
        _mapper.Map<UserAddressDto>(Arg.Any<UserAddress>()).Returns(expectedDto);

        var result = await _sut.Handle(BuildCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);
        user.Addresses.Count.ShouldBe(1);
        user.DefaultAddressId.ShouldBeNull();
        _userRepository.Received(1).Update(user);
        _mapper.Received(1).Map<UserAddressDto>(Arg.Any<UserAddress>());
    }

    [Fact]
    public async Task Handle_WhenIsDefaultTrue_SetsAddressAsDefault()
    {
        var user = new UserBuilder().Build();
        _userRepository
            .GetWithAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _mapper.Map<UserAddressDto>(Arg.Any<UserAddress>()).Returns(new UserAddressDto());

        var result = await _sut.Handle(BuildCommand(isDefault: true), CancellationToken.None);

        result.ShouldBeSuccess();
        user.Addresses.Count.ShouldBe(1);
        var address = user.Addresses[0];
        user.DefaultAddressId.ShouldBe(address.Id);
        address.IsDefault.ShouldBeTrue();
        _userRepository.Received(1).Update(user);
    }
}
