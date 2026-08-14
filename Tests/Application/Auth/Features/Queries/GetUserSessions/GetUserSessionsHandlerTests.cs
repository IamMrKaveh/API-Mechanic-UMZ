using Application.Auth.Features.Queries.GetUserSessions;
using Application.Auth.Features.Shared;
using Application.Common.Interfaces;
using Application.User.Contracts;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Auth.Features.Queries.GetUserSessions;

public class GetUserSessionsHandlerTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetUserSessionsHandler _sut;

    public GetUserSessionsHandlerTests()
    {
        _sut = new GetUserSessionsHandler(_userQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenTargetAndCallerAreNull_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetUserSessionsQuery(null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _userQueryService.DidNotReceiveWithAnyArgs().GetActiveSessionsAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenTargetIsEmptyGuidAndCallerIsNull_ReturnsUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetUserSessionsQuery(Guid.Empty), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _userQueryService.DidNotReceiveWithAnyArgs().GetActiveSessionsAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenTargetProvided_UsesTargetUserIdAndReturnsPaginatedSessions()
    {
        var targetGuid = Guid.NewGuid();
        var currentSessionGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.SessionId.Returns((Guid?)currentSessionGuid);

        var sessions = new List<UserSessionDto>
    {
        new() { Id = Guid.NewGuid() },
        new() { Id = Guid.NewGuid() }
    };
        _userQueryService
            .GetActiveSessionsAsync(
                Arg.Is<UserId>(x => x == UserId.From(targetGuid)),
                currentSessionGuid,
                Arg.Any<CancellationToken>())
            .Returns(sessions);

        var result = await _sut.Handle(new GetUserSessionsQuery(targetGuid), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WhenTargetIsNullAndCallerAuthenticated_FallsBackToCallerId()
    {
        var callerGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)callerGuid);
        _currentUserService.SessionId.Returns((Guid?)null);
        _userQueryService
            .GetActiveSessionsAsync(
                Arg.Any<UserId>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<UserSessionDto>());

        var result = await _sut.Handle(new GetUserSessionsQuery(null), CancellationToken.None);

        result.ShouldBeSuccess();
        await _userQueryService.Received(1).GetActiveSessionsAsync(
            Arg.Is<UserId>(x => x == UserId.From(callerGuid)),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSessionsEmpty_ReturnsSuccessWithEmptyPaginatedResult()
    {
        var callerGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)callerGuid);
        _userQueryService
            .GetActiveSessionsAsync(
                Arg.Any<UserId>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<UserSessionDto>());

        var result = await _sut.Handle(new GetUserSessionsQuery(null), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }
}
