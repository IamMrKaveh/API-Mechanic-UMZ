using Application.Auth.Contracts;
using Application.Cache.Contracts;
using Domain.Security.Aggregates;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Infrastructure.Auth.Services;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Auth.Services;

public class SessionRevocationCheckerTests : HandlerTestBase
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly SessionRevocationChecker _sut;

    public SessionRevocationCheckerTests()
    {
        _sut = new SessionRevocationChecker(_sessionRepository, _cacheService, DateTimeProvider);
    }

    [Fact]
    public async Task IsSessionActiveAsync_WhenSessionActive_ReturnsTrueAndCaches()
    {
        var sessionId = SessionId.NewId();
        var session = new UserSessionBuilder()
            .WithId(sessionId)
            .WithExpiresAt(DateTime.UtcNow.AddDays(1))
            .Build();
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.IsSessionActiveAsync(sessionId.Value);

        result.ShouldBeTrue();
        await _cacheService.Received(1).SetAsync(
            Arg.Is<string>(k => k!.Contains(sessionId.Value.ToString("N"))),
            true,
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsSessionActiveAsync_WhenSessionRevoked_ReturnsFalse()
    {
        var sessionId = SessionId.NewId();
        var session = new UserSessionBuilder()
            .WithId(sessionId)
            .WithExpiresAt(DateTime.UtcNow.AddDays(1))
            .Build();
        session.Revoke(DateTime.UtcNow);
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.IsSessionActiveAsync(sessionId.Value);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsSessionActiveAsync_WhenSessionMissing_ReturnsFalse()
    {
        var sessionId = SessionId.NewId();
        _sessionRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        var result = await _sut.IsSessionActiveAsync(sessionId.Value);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsSessionActiveAsync_WhenCachedVerdict_DoesNotQueryRepository()
    {
        var sessionId = SessionId.NewId();
        _cacheService.GetAsync<bool?>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.IsSessionActiveAsync(sessionId.Value);

        result.ShouldBeFalse();
        await _sessionRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }
}
