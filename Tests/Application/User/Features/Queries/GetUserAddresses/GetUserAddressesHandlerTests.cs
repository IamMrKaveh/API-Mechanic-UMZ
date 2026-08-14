using Application.Common.Interfaces;
using Application.User.Contracts;
using Application.User.Features.Queries.GetUserAddresses;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.User.Features.Queries.GetUserAddresses;

public class GetUserAddressesHandlerTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetUserAddressesHandler _sut;

    public GetUserAddressesHandlerTests()
    {
        _sut = new GetUserAddressesHandler(_userQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_ReturnsAddressesForCurrentUser()
    {
        var userGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userGuid);

        var addresses = new List<UserAddressDto>
    {
        new() { Id = Guid.NewGuid(), Title = "Home" },
        new() { Id = Guid.NewGuid(), Title = "Work" }
    };

        _userQueryService
            .GetUserAddressesAsync(Arg.Is<UserId>(x => x == UserId.From(userGuid)), Arg.Any<CancellationToken>())
            .Returns(addresses);

        var result = await _sut.Handle(new GetUserAddressesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(addresses);
    }

    [Fact]
    public async Task Handle_WhenNoAddresses_ReturnsSuccessWithEmpty()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _userQueryService
            .GetUserAddressesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserAddressDto>());

        var result = await _sut.Handle(new GetUserAddressesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
