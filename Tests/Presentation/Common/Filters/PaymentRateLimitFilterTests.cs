using Application.Common.Interfaces;
using Application.Security.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Filters;

namespace Tests.Presentation.Common.Filters;

public class PaymentRateLimitFilterTests
{
    private readonly IRateLimitService _rateLimit = Substitute.For<IRateLimitService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static ActionExecutingContext BuildContext(IServiceProvider services, string ip = "1.2.3.4")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        httpContext.RequestServices = services;
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_rateLimit);
        services.AddSingleton(_currentUser);
        return services.BuildServiceProvider();
    }

    private static ActionExecutionDelegate Next(bool[] called, ActionExecutingContext ctx) => () =>
    {
        called[0] = true;
        return Task.FromResult(new ActionExecutedContext(
            ctx, new List<IFilterMetadata>(), new object()));
    };

    [Fact]
    public async Task OnActionExecutionAsync_WhenNotLimited_CallsNext()
    {
        _rateLimit.IsLimitedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((false, null));
        _currentUser.IsAuthenticated.Returns(false);
        var context = BuildContext(BuildServices());
        var called = new[] { false };

        await new PaymentRateLimitAttribute().OnActionExecutionAsync(context, Next(called, context));

        called[0].ShouldBeTrue();
        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenAuthenticated_UsesUserBasedKey()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        string? capturedKey = null;
        int capturedMax = 0, capturedWindow = 0;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Do<int>(m => capturedMax = m), Arg.Do<int>(w => capturedWindow = w))
            .Returns((false, null));
        var context = BuildContext(BuildServices());

        await new PaymentRateLimitAttribute().OnActionExecutionAsync(context, Next(new[] { false }, context));

        capturedKey.ShouldBe($"payment_limit_user_{userId}");
        capturedMax.ShouldBe(3);
        capturedWindow.ShouldBe(10);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenAnonymous_UsesIpBasedKey()
    {
        _currentUser.IsAuthenticated.Returns(false);
        string? capturedKey = null;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Any<int>(), Arg.Any<int>())
            .Returns((false, null));
        var context = BuildContext(BuildServices(), ip: "5.6.7.8");

        await new PaymentRateLimitAttribute().OnActionExecutionAsync(context, Next(new[] { false }, context));

        capturedKey.ShouldBe("payment_limit_ip_5.6.7.8");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenLimited_Returns429WithoutCallingNext()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _rateLimit.IsLimitedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((true, TimeSpan.FromSeconds(90)));
        var context = BuildContext(BuildServices());
        var called = new[] { false };

        await new PaymentRateLimitAttribute().OnActionExecutionAsync(context, Next(called, context));

        called[0].ShouldBeFalse();
        var obj = context.Result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }
}
