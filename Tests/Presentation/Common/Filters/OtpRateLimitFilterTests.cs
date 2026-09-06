using Application.Security.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using Presentation.Common.Filters;

namespace Tests.Presentation.Common.Filters;

public class OtpRateLimitFilterTests
{
    private readonly IRateLimitService _rateLimit = Substitute.For<IRateLimitService>();
    private readonly ILogger<OtpRateLimitFilter> _logger = Substitute.For<ILogger<OtpRateLimitFilter>>();

    private OtpRateLimitFilter BuildFilter() => new(_rateLimit, _logger);

    private static ActionExecutingContext BuildContext(bool withAttribute, string ip = "1.2.3.4")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        var descriptor = new ControllerActionDescriptor();
        if (withAttribute)
            descriptor.EndpointMetadata = new List<object> { new OtpRateLimitAttribute() };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ActionExecutionDelegate Next(bool[] called) => () =>
    {
        called[0] = true;
        var ctx = BuildContext(false);
        return Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), new object()));
    };

    [Fact]
    public async Task OnActionExecutionAsync_WithoutAttribute_CallsNextWithoutCheckingLimit()
    {
        var context = BuildContext(withAttribute: false);
        var called = new[] { false };

        await BuildFilter().OnActionExecutionAsync(context, Next(called));

        called[0].ShouldBeTrue();
        context.Result.ShouldBeNull();
        await _rateLimit.DidNotReceiveWithAnyArgs().IsLimitedAsync(default!, default, default);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAttributeAndNotLimited_CallsNext()
    {
        _rateLimit.IsLimitedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((false, null));
        var context = BuildContext(withAttribute: true);
        var called = new[] { false };

        await BuildFilter().OnActionExecutionAsync(context, Next(called));

        called[0].ShouldBeTrue();
        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithAttributeAndNotLimited_ChecksIpBasedKeyWithLimits()
    {
        string? capturedKey = null;
        int capturedMax = 0, capturedWindow = 0;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Do<int>(m => capturedMax = m), Arg.Do<int>(w => capturedWindow = w))
            .Returns((false, null));
        var context = BuildContext(withAttribute: true, ip: "9.8.7.6");

        await BuildFilter().OnActionExecutionAsync(context, Next(new[] { false }));

        capturedKey.ShouldBe("otp_rl_9.8.7.6");
        capturedMax.ShouldBe(5);
        capturedWindow.ShouldBe(10);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenLimited_Returns429WithoutCallingNext()
    {
        _rateLimit.IsLimitedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((true, TimeSpan.FromSeconds(45)));
        var context = BuildContext(withAttribute: true);
        var called = new[] { false };

        await BuildFilter().OnActionExecutionAsync(context, Next(called));

        called[0].ShouldBeFalse();
        var obj = context.Result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.HttpContext.Response.Headers["Retry-After"].ToString().ShouldBe("00:00:45");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenLimitedAndServiceThrows_Propagates()
    {
        _rateLimit.IsLimitedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .ThrowsAsync(new InvalidOperationException("redis down"));
        var context = BuildContext(withAttribute: true);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            BuildFilter().OnActionExecutionAsync(context, Next(new[] { false })));
    }
}
