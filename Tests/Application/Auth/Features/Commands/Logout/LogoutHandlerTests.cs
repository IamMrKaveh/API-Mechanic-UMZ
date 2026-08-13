using Application.Auth.Features.Commands.Logout;
using Application.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using RefreshTokens = Domain.Security.ValueObjects.RefreshToken;

namespace Tests.Application.Auth.Features.Commands.Logout;

public class LogoutHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly LogoutHandler _sut;

    public LogoutHandlerTests()
    {
        _sut = new LogoutHandler(_sessionRepository, _currentUser);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenRefreshTokenIsMissing_ReturnsSuccessWithoutCallingRepository(string refreshToken)
    {
        var result = await _sut.Handle(new LogoutCommand(refreshToken), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.DidNotReceiveWithAnyArgs().GetByRefreshTokenAsync(default!, default);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNull_ReturnsSuccessWithoutCallingRepository()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var refreshToken = RefreshTokens.Generate().Value;

        var result = await _sut.Handle(new LogoutCommand(refreshToken), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.DidNotReceiveWithAnyArgs().GetByRefreshTokenAsync(default!, default);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFoundForRefreshToken_ReturnsSuccessWithoutUpdating()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _sessionRepository
            .GetByRefreshTokenAsync(Arg.Any<RefreshTokens>(), Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        var result = await _sut.Handle(new LogoutCommand(RefreshTokens.Generate().Value), CancellationToken.None);

        result.ShouldBeSuccess();
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToAnotherUser_ReturnsSuccessWithoutUpdating()
    {
        var callerGuid = Guid.NewGuid();
        var ownerUserId = UserId.NewId();
        var session = new UserSessionBuilder().WithUserId(ownerUserId).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _sessionRepository
            .GetByRefreshTokenAsync(Arg.Any<RefreshTokens>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _sut.Handle(new LogoutCommand(session.RefreshToken.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        session.IsRevoked.ShouldBeFalse();
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToCaller_RevokesWithUserRequestedAndUpdates()
    {
        var callerGuid = Guid.NewGuid();
        var callerUserId = UserId.From(callerGuid);
        var session = new UserSessionBuilder().WithUserId(callerUserId).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _sessionRepository
            .GetByRefreshTokenAsync(Arg.Any<RefreshTokens>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _sut.Handle(new LogoutCommand(session.RefreshToken.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        session.IsRevoked.ShouldBeTrue();
        session.RevocationReason.ShouldBe(SessionRevocationReason.UserRequested);
        _sessionRepository.Received(1).Update(session);
    }
}
