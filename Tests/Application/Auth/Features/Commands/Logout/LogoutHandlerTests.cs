using Domain.Security.Services;
using Application.Auth.Features.Commands.Logout;
using Application.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using RefreshTokens = Domain.Security.ValueObjects.RefreshToken;

namespace Tests.Application.Auth.Features.Commands.Logout;

public class LogoutHandlerTests : HandlerTestBase
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly LogoutHandler _sut;

    public LogoutHandlerTests()
    {
        SetUtcNow(2026, 8, 29, 10);
        _sut = new LogoutHandler(_sessionRepository, CurrentUserService, DateTimeProvider);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenRefreshTokenIsMissing_ReturnsSuccessWithoutCallingRepository(string refreshToken)
    {
        var result = await _sut.Handle(new LogoutCommand(refreshToken), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.DidNotReceiveWithAnyArgs().GetByRefreshTokenHashAsync(default!, default);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNull_ReturnsSuccessWithoutCallingRepository()
    {
        CurrentUserService.UserId.Returns((Guid?)null);
        var refreshToken = RefreshTokens.Generate().Value;

        var result = await _sut.Handle(new LogoutCommand(refreshToken), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.DidNotReceiveWithAnyArgs().GetByRefreshTokenHashAsync(default!, default);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFoundForRefreshToken_ReturnsSuccessWithoutUpdating()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sessionRepository
            .GetByRefreshTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
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
        var token = RefreshTokens.Generate();
        var session = new UserSessionBuilder().WithUserId(ownerUserId).WithRefreshToken(token).Build();

        CurrentUserService.UserId.Returns((Guid?)callerGuid);
        _sessionRepository
            .GetByRefreshTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _sut.Handle(new LogoutCommand(token.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        session.IsRevoked.ShouldBeFalse();
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToCaller_RevokesWithUserRequestedAndUpdates()
    {
        var callerGuid = Guid.NewGuid();
        var callerUserId = UserId.From(callerGuid);
        var token = RefreshTokens.Generate();
        var session = new UserSessionBuilder().WithUserId(callerUserId).WithRefreshToken(token).Build();

        CurrentUserService.UserId.Returns((Guid?)callerGuid);
        _sessionRepository
            .GetByRefreshTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _sut.Handle(new LogoutCommand(token.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        session.IsRevoked.ShouldBeTrue();
        session.RevocationReason.ShouldBe(SessionRevocationReason.UserRequested);
        _sessionRepository.Received(1).Update(session);
    }
}

