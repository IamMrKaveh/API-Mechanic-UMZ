using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Services;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Auth.Services;

public class AuthServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly ISessionService _sessionService = Substitute.For<ISessionService>(); private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>(); private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepository, _sessionRepository, _sessionService, _jwtTokenGenerator);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenExistingSessionNotFound_ReturnsUnauthorized()
    {
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        var result = await _sut.RefreshTokenAsync(refreshToken, ip, "agent");

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _sessionService.DidNotReceiveWithAnyArgs().RefreshSessionAsync(default!, default!, default);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenExistingSessionRevoked_ReturnsUnauthorized()
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

        var result = await _sut.RefreshTokenAsync(refreshToken, ip, "agent");

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _sessionService.DidNotReceiveWithAnyArgs().RefreshSessionAsync(default!, default!, default);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenSessionServiceRefreshFails_PropagatesFailure()
    {
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");

        var existingSession = new UserSessionBuilder()
            .WithRefreshToken(refreshToken)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(existingSession);

        _sessionService
            .RefreshSessionAsync(refreshToken, ip, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Unauthorized("توکن نامعتبر است."));

        var result = await _sut.RefreshTokenAsync(refreshToken, ip, "agent");

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _userRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");
        var userId = UserId.NewId();

        var existingSession = new UserSessionBuilder()
            .WithUserId(userId)
            .WithRefreshToken(refreshToken)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(existingSession);

        _sessionService
            .RefreshSessionAsync(refreshToken, ip, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(
                new RefreshTokenResult(Guid.NewGuid(), RefreshToken.Generate().Value, DateTime.UtcNow.AddDays(30), userId.Value)));

        _userRepository
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.RefreshTokenAsync(refreshToken, ip, "agent");

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenUserIsInactive_ReturnsUnauthorized()
    {
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");

        var user = new UserBuilder()
            .WithPhoneNumber(new PhoneNumberBuilder().Build())
            .Build();
        user.Deactivate();

        var existingSession = new UserSessionBuilder()
            .WithUserId(user.Id)
            .WithRefreshToken(refreshToken)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(existingSession);

        _sessionService
            .RefreshSessionAsync(refreshToken, ip, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(
                new RefreshTokenResult(Guid.NewGuid(), RefreshToken.Generate().Value, DateTime.UtcNow.AddDays(30), user.Id.Value)));

        _userRepository
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.RefreshTokenAsync(refreshToken, ip, "agent");

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenAllValid_ReturnsAccessTokenAndRefreshedSession()
    {
        var refreshToken = RefreshToken.Generate();
        var ip = IpAddress.Create("10.0.0.1");

        var user = new UserBuilder()
            .WithPhoneNumber(new PhoneNumberBuilder().Build())
            .Build();

        var existingSession = new UserSessionBuilder()
            .WithUserId(user.Id)
            .WithRefreshToken(refreshToken)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        _sessionRepository
            .GetByRefreshTokenAsync(refreshToken, Arg.Any<CancellationToken>())
            .Returns(existingSession);

        var newSessionId = Guid.NewGuid();
        var newRefreshTokenValue = RefreshToken.Generate().Value;
        var newExpiresAt = DateTime.UtcNow.AddDays(30);

        _sessionService
            .RefreshSessionAsync(refreshToken, ip, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(
                new RefreshTokenResult(newSessionId, newRefreshTokenValue, newExpiresAt, user.Id.Value)));

        _userRepository
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        _jwtTokenGenerator
            .GenerateAccessToken(user, Arg.Is<SessionId>(s => s!.Value == newSessionId))
            .Returns("test-access-token");

        var result = await _sut.RefreshTokenAsync(refreshToken, ip, "agent");

        result.ShouldBeSuccess();
        result.Value.AccessToken.ShouldBe("test-access-token");
        result.Value.RefreshToken.SessionId.ShouldBe(newSessionId);
        result.Value.RefreshToken.RefreshToken.ShouldBe(newRefreshTokenValue);
        result.Value.RefreshToken.ExpiresAt.ShouldBe(newExpiresAt);
        result.Value.User.ShouldNotBeNull();
        result.Value.IsNewUser.ShouldBeFalse();
    }
}
