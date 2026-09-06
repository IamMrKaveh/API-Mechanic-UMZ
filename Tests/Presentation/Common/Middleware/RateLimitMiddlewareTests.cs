using Application.Common.Interfaces;
using Application.Security.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Presentation.Common.Middleware;

namespace Tests.Presentation.Common.Middleware;

public class RateLimitMiddlewareTests
{
    private readonly IRateLimitService _rateLimit = Substitute.For<IRateLimitService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILogger<RateLimitMiddleware> _logger =
        Substitute.For<ILogger<RateLimitMiddleware>>();

    private RateLimitMiddleware BuildSut(RequestDelegate next)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_rateLimit);
        services.AddSingleton(_currentUser);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_ => services.BuildServiceProvider().CreateScope());
        return new RateLimitMiddleware(next, _logger, scopeFactory);
    }

    private static DefaultHttpContext BuildContext(string? ip)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = ip is null ? null : IPAddress.Parse(ip);
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenIpMissing_CallsNextWithoutCheckingLimit()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext(null));

        called.ShouldBeTrue();
        await _rateLimit.DidNotReceiveWithAnyArgs().IsLimitedAsync(default!, default, default);
    }

    [Fact]
    public async Task InvokeAsync_AnonymousUser_UsesIpKeyWithAnonymousLimit()
    {
        _currentUser.IsAuthenticated.Returns(false);
        string? capturedKey = null;
        int capturedMax = 0, capturedWindow = 0;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Do<int>(m => capturedMax = m), Arg.Do<int>(w => capturedWindow = w))
            .Returns((false, null));
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext("3.3.3.3"));

        called.ShouldBeTrue();
        capturedKey.ShouldBe("rl_ip_3.3.3.3");
        capturedMax.ShouldBe(100);
        capturedWindow.ShouldBe(1);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUser_UsesUserKeyWithAuthenticatedLimit()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        string? capturedKey = null;
        int capturedMax = 0;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Do<int>(m => capturedMax = m), Arg.Any<int>())
            .Returns((false, null));
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext("3.3.3.3"));

        called.ShouldBeTrue();
        capturedKey.ShouldBe($"rl_user_{userId}");
        capturedMax.ShouldBe(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenLimited_Returns429WithRetryAfter()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _rateLimit.IsLimitedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((true, TimeSpan.FromSeconds(20)));
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });
        var context = BuildContext("4.4.4.4");

        await sut.InvokeAsync(context);

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.Response.Headers["Retry-After"].ToString().ShouldBe("00:00:20");
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        new StreamReader(context.Response.Body).ReadToEnd()
            .ShouldBe("Too many requests. Please try again later.");
    }
}
