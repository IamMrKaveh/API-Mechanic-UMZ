using Application.Cache.Contracts;
using Application.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Presentation.Common.Middleware;
using System.Security.Claims;
using Tests.TestInfrastructure.Builders;

namespace Tests.Presentation.Common.Middleware;

public class SessionActivityMiddlewareTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly ISessionRepository _sessions = Substitute.For<ISessionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SessionActivityMiddleware BuildSut(RequestDelegate next)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_currentUser);
        services.AddSingleton(_cache);
        services.AddSingleton(_sessions);
        services.AddSingleton(_unitOfWork);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_ => services.BuildServiceProvider().CreateScope());
        return new SessionActivityMiddleware(
            next,
            Substitute.For<ILogger<SessionActivityMiddleware>>(),
            scopeFactory);
    }

    private static DefaultHttpContext BuildContext(bool authenticated)
    {
        var context = new DefaultHttpContext();
        context.User = authenticated
            ? new ClaimsPrincipal(new ClaimsIdentity("test"))
            : new ClaimsPrincipal();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenAnonymous_CallsNextOnly()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(authenticated: false));

        called.ShouldBeTrue();
        await _sessions.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenSessionIdIsNull_CallsNextOnly()
    {
        _currentUser.SessionId.Returns((Guid?)null);
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(authenticated: true));

        called.ShouldBeTrue();
        await _sessions.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenSessionIdIsEmpty_CallsNextOnly()
    {
        _currentUser.SessionId.Returns(Guid.Empty);
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(authenticated: true));

        called.ShouldBeTrue();
        await _sessions.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenCacheHit_SkipsRepositoryUpdate()
    {
        var sessionId = Guid.NewGuid();
        _currentUser.SessionId.Returns(sessionId);
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(authenticated: true));

        called.ShouldBeTrue();
        await _sessions.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
        await _cache.Received(1).ExistsAsync(
            $"session:activity:{sessionId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenActiveSession_UpdatesActivityAndSetsCache()
    {
        var sessionId = Guid.NewGuid();
        _currentUser.SessionId.Returns(sessionId);
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var session = new UserSessionBuilder()
            .WithId(SessionId.From(sessionId))
            .Build();
        _sessions.GetByIdAsync(SessionId.From(sessionId), Arg.Any<CancellationToken>())
            .Returns(session);
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(authenticated: true));

        called.ShouldBeTrue();
        _sessions.Received(1).Update(session);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            $"session:activity:{sessionId}", true, TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvokeAsync_WhenSessionNotFound_DoesNothing()
    {
        var sessionId = Guid.NewGuid();
        _currentUser.SessionId.Returns(sessionId);
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _sessions.GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);
        var sut = BuildSut(_ => Task.CompletedTask);

        await sut.InvokeAsync(BuildContext(authenticated: true));

        _sessions.DidNotReceive().Update(Arg.Any<UserSession>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task InvokeAsync_WhenSessionInactive_DoesNothing()
    {
        var sessionId = Guid.NewGuid();
        _currentUser.SessionId.Returns(sessionId);
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var revoked = new UserSessionBuilder()
            .WithId(SessionId.From(sessionId))
            .Build();
        revoked.Revoke();
        _sessions.GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(revoked);
        var sut = BuildSut(_ => Task.CompletedTask);

        await sut.InvokeAsync(BuildContext(authenticated: true));

        _sessions.DidNotReceive().Update(Arg.Any<UserSession>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task InvokeAsync_WhenCacheThrowsCancellation_SwallowsAndStillCallsNext()
    {
        var sessionId = Guid.NewGuid();
        _currentUser.SessionId.Returns(sessionId);
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new OperationCanceledException()));
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(authenticated: true));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenRepositoryThrows_SwallowsAndStillCallsNext()
    {
        var sessionId = Guid.NewGuid();
        _currentUser.SessionId.Returns(sessionId);
        _cache.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _sessions.GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserSession?>(new InvalidOperationException("db down")));
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(authenticated: true));

        called.ShouldBeTrue();
    }
}
