using Application.Auth.Contracts;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Shared;
using Application.Common.Interfaces;
using Application.User.Features.Shared;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using RefreshTokens = Domain.Security.ValueObjects.RefreshToken;

namespace Tests.Application.Auth.Features.Commands.RefreshToken;

public class RefreshTokenHandlerTests : HandlerTestBase
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>(); private readonly DateTime _now = new(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc); private readonly RefreshTokenHandler _sut;

    public RefreshTokenHandlerTests()
    {
        DateTimeProvider.UtcNow.Returns(_now);

        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "test-signing-key-with-at-least-32-characters-length",
            Issuer = "mechanic-tests-issuer",
            Audience = "mechanic-tests-audience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 30
        });

        _sut = new RefreshTokenHandler(_authService, CurrentUserService, DateTimeProvider, jwtOptions);
    }

    [Fact]
    public async Task Handle_WhenAuthServiceFails_ReturnsFailureWithSameError()
    {
        CurrentUserService.IpAddress.Returns("127.0.0.1");
        CurrentUserService.UserAgent.Returns("agent");

        var incoming = RefreshTokens.Generate();
        _authService
            .RefreshTokenAsync(
                Arg.Any<RefreshTokens>(),
                Arg.Any<IpAddress>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>
                .Unauthorized("توکن نامعتبر است."));

        var result = await _sut.Handle(new RefreshTokenCommand(incoming.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenIpAddressIsMissing_UsesUnknownIp()
    {
        CurrentUserService.IpAddress.Returns((string?)null);
        CurrentUserService.UserAgent.Returns((string?)null);

        var incoming = RefreshTokens.Generate();

        var refreshTokenResult = new RefreshTokenResult(
            Guid.NewGuid(),
            RefreshTokens.Generate().Value,
            DateTime.UtcNow.AddDays(30),
            Guid.NewGuid());

        var userDto = new UserProfileDto { Id = Guid.NewGuid() };
        var tuple = ("access-token", refreshTokenResult, userDto, false);

        _authService
            .RefreshTokenAsync(
                Arg.Any<RefreshTokens>(),
                Arg.Any<IpAddress>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Success(tuple));

        var result = await _sut.Handle(new RefreshTokenCommand(incoming.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        await _authService.Received(1).RefreshTokenAsync(
            Arg.Any<RefreshTokens>(),
            Arg.Is<IpAddress>(x => x == IpAddress.Unknown),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthServiceSucceeds_ReturnsAuthResultWithMappedFields()
    {
        CurrentUserService.IpAddress.Returns("10.0.0.5");
        CurrentUserService.UserAgent.Returns("test-agent");

        var incoming = RefreshTokens.Generate();
        var newRefreshToken = RefreshTokens.Generate();
        var refreshExpiresAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var refreshTokenResult = new RefreshTokenResult(
            Guid.NewGuid(),
            newRefreshToken.Value,
            refreshExpiresAt,
            Guid.NewGuid());

        var userDto = new UserProfileDto { Id = Guid.NewGuid() };
        var tuple = ("access-token-value", refreshTokenResult, userDto, true);

        _authService
            .RefreshTokenAsync(
                Arg.Is<RefreshTokens>(x => x == incoming),
                Arg.Is<IpAddress>(x => x == IpAddress.Create("10.0.0.5")),
                "test-agent",
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Success(tuple));

        var result = await _sut.Handle(new RefreshTokenCommand(incoming.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.AccessToken.ShouldBe("access-token-value");
        result.Value.RefreshToken.ShouldBe(newRefreshToken.Value);
        result.Value.RefreshTokenExpiresAt.ShouldBe(refreshExpiresAt);
        result.Value.User.ShouldBeSameAs(userDto);
        result.Value.IsNewUser.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenAuthServiceSucceeds_AccessTokenExpiresAtMatchesJwtOptions()
    {
        CurrentUserService.IpAddress.Returns("10.0.0.5");
        CurrentUserService.UserAgent.Returns("test-agent");

        var incoming = RefreshTokens.Generate();
        var refreshTokenResult = new RefreshTokenResult(
            Guid.NewGuid(),
            RefreshTokens.Generate().Value,
            _now.AddDays(30),
            Guid.NewGuid());

        var userDto = new UserProfileDto { Id = Guid.NewGuid() };
        var tuple = ("access-token-value", refreshTokenResult, userDto, false);

        _authService
            .RefreshTokenAsync(
                Arg.Any<RefreshTokens>(),
                Arg.Any<IpAddress>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Success(tuple));

        var result = await _sut.Handle(new RefreshTokenCommand(incoming.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.AccessTokenExpiresAt.ShouldBe(_now.AddMinutes(60));
    }
}
