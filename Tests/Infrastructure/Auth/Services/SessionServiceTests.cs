using Application.Auth.Features.Shared;
using Application.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Services;
using Microsoft.Extensions.Options;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Auth.Services;

public class SessionServiceTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly SessionService _sut;

    public SessionServiceTests()
    {
        var options = Options.Create(new AuthOptions
        {
            SessionExpirationDays = 30,
        });

        _sut = new SessionService(_sessionRepository, options, _unitOfWork);
    }

    [Fact]
    public async Task CreateSessionAsync_WhenNoExistingSessionForDevice_AddsNewSessionAndSaves()
    {
        var userId = UserId.NewId();
        var ip = IpAddress.Create("10.0.0.1");
        var userAgent = "chrome-mac";

        _sessionRepository
            .GetActiveByUserAndDeviceAsync(userId, Arg.Any<DeviceInfo>(), Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        var result = await _sut.CreateSessionAsync(userId, ip, userAgent);

        result.ShouldBeSuccess();
        await _sessionRepository.Received(1).AddAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSessionAsync_WhenExistingActiveSessionForDevice_RevokesAndReplacesIt()
    {
        var userId = UserId.NewId();
        var ip = IpAddress.Create("10.0.0.1");
        var userAgent = "chrome-mac";

        var existing = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo(userAgent)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        _sessionRepository
            .GetActiveByUserAndDeviceAsync(userId, Arg.Any<DeviceInfo>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.CreateSessionAsync(userId, ip, userAgent);

        result.ShouldBeSuccess();
        existing.IsRevoked.ShouldBeTrue();
        existing.RevocationReason.ShouldBe(SessionRevocationReason.UserRequested);
        _sessionRepository.Received(1).Update(existing);
        await _sessionRepository.Received(1).AddAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateSessionAsync_WithNullUserAgent_UsesUnknownDeviceInfo()
    {
        var userId = UserId.NewId();
        var ip = IpAddress.Create("10.0.0.1");

        _sessionRepository
            .GetActiveByUserAndDeviceAsync(userId, Arg.Any<DeviceInfo>(), Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        UserSession? captured = null;
        await _sessionRepository.AddAsync(Arg.Do<UserSession>(s => captured = s), Arg.Any<CancellationToken>());

        var result = await _sut.CreateSessionAsync(userId, ip, null);

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();
        captured!.DeviceInfo.Value.ShouldBe("Unknown");
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsRefreshTokenResultWithSessionAndUserMetadata()
    {
        var userId = UserId.NewId();
        var ip = IpAddress.Create("10.0.0.1");

        _sessionRepository
            .GetActiveByUserAndDeviceAsync(userId, Arg.Any<DeviceInfo>(), Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        UserSession? captured = null;
        await _sessionRepository.AddAsync(Arg.Do<UserSession>(s => captured = s), Arg.Any<CancellationToken>());

        var before = DateTime.UtcNow;
        var result = await _sut.CreateSessionAsync(userId, ip, "agent");

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();

        result.Value.SessionId.ShouldBe(captured!.Id.Value);
        result.Value.UserId.ShouldBe(userId.Value);
        result.Value.RefreshToken.ShouldBe(captured.RefreshToken.Value);
        result.Value.ExpiresAt.ShouldBeGreaterThanOrEqualTo(before.AddDays(30).AddSeconds(-5));
    }

    [Fact]
    public async Task RefreshSessionAsync_WhenSessionNotFound_ReturnsUnauthorizedAndDoesNotSave()
    {
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        var result = await _sut.RefreshSessionAsync(refreshToken, ip);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _sessionRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task RefreshSessionAsync_WhenSessionRevoked_ReturnsUnauthorized()
    {
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");

        var session = new UserSessionBuilder()
            .WithRefreshToken(refreshToken)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();
        session.Revoke(SessionRevocationReason.UserRequested);

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _sut.RefreshSessionAsync(refreshToken, ip);

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshSessionAsync_WhenSessionActive_RevokesOldAndCreatesNewSession()
    {
        var userId = UserId.NewId();
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");

        var existing = new UserSessionBuilder()
            .WithUserId(userId)
            .WithRefreshToken(refreshToken)
            .WithDeviceInfo("device-x")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(existing);

        UserSession? newSession = null;
        await _sessionRepository.AddAsync(Arg.Do<UserSession>(s => newSession = s), Arg.Any<CancellationToken>());

        var result = await _sut.RefreshSessionAsync(refreshToken, ip);

        result.ShouldBeSuccess();
        existing.IsRevoked.ShouldBeTrue();
        _sessionRepository.Received(1).Update(existing);

        newSession.ShouldNotBeNull();
        newSession!.UserId.ShouldBe(userId);
        newSession.DeviceInfo.Value.ShouldBe("device-x");
        newSession.RefreshToken.Value.ShouldNotBe(refreshToken.Value);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_DelegatesToRepositoryAndSaves()
    {
        var userId = UserId.NewId();

        await _sut.RevokeAllSessionsAsync(userId);

        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(userId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
