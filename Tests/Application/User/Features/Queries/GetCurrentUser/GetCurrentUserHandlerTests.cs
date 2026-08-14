using Application.Common.Interfaces;
using Application.User.Contracts;
using Application.User.Features.Queries.GetCurrentUser;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.User.Features.Queries.GetCurrentUser;

public class GetCurrentUserHandlerTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetCurrentUserHandler _sut;

    public GetCurrentUserHandlerTests()
    {
        _sut = new GetCurrentUserHandler(_userQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_ReturnsSuccessWithProfile()
    {
        var userGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userGuid);
        var expected = new UserProfileDto { Id = userGuid };

        _userQueryService
            .GetUserProfileAsync(Arg.Is<UserId>(x => x == UserId.From(userGuid)), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _userQueryService
            .GetUserProfileAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UserProfileDto?)null);

        var result = await _sut.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }
}
