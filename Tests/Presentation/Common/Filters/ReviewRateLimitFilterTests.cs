using Application.Common.Interfaces;
using Application.Review.Configuration;
using Application.Security.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Presentation.Common.Filters;

namespace Tests.Presentation.Common.Filters;

public class ReviewRateLimitFilterTests
{
    private readonly IRateLimitService _rateLimit = Substitute.For<IRateLimitService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILogger<ReviewRateLimitFilter> _logger = Substitute.For<ILogger<ReviewRateLimitFilter>>();

    private static IOptions<ReviewSettings> BuildSettings(
        int create = 5,
        int reads = 60,
        int admin = 30,
        int vote = 20) =>
        Options.Create(new ReviewSettings
        {
            RateLimit = new ReviewRateLimitSettings
            {
                CreateReviewPerMinute = create,
                PublicReadsPerMinute = reads,
                AdminActionsPerMinute = admin,
                VotePerMinute = vote
            }
        });

    private ReviewRateLimitFilter BuildFilter(IOptions<ReviewSettings>? settings = null) =>
        new(_rateLimit, _currentUser, settings ?? BuildSettings(), _logger);

    private static ActionExecutingContext BuildContext(ReviewRateLimitPolicy? policy, string ip = "1.2.3.4")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        var descriptor = new ControllerActionDescriptor();
        if (policy.HasValue)
            descriptor.EndpointMetadata = new List<object> { new ReviewRateLimitAttribute(policy.Value) };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ActionExecutionDelegate Next(bool[] called, ActionExecutingContext ctx) => () =>
    {
        called[0] = true;
        return Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), new object()));
    };

    [Fact]
    public async Task OnActionExecutionAsync_WithoutAttribute_CallsNextWithoutCheckingLimit()
    {
        var context = BuildContext(null);
        var called = new[] { false };

        await BuildFilter().OnActionExecutionAsync(context, Next(called, context));

        called[0].ShouldBeTrue();
        context.Result.ShouldBeNull();
        await _rateLimit.DidNotReceiveWithAnyArgs().IsLimitedAsync(default!, default, default);
    }

    [Theory]
    [InlineData(ReviewRateLimitPolicy.CreateReview, 7, "review_create_user_", 7)]
    [InlineData(ReviewRateLimitPolicy.AdminAction, 11, "review_admin_user_", 11)]
    [InlineData(ReviewRateLimitPolicy.Vote, 13, "review_vote_user_", 13)]
    public async Task OnActionExecutionAsync_AuthenticatedUser_UsesUserSegmentAndConfiguredLimit(
        ReviewRateLimitPolicy policy, int limit, string keyPrefix, int expectedMax)
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        string? capturedKey = null;
        int capturedMax = 0, capturedWindow = 0;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Do<int>(m => capturedMax = m), Arg.Do<int>(w => capturedWindow = w))
            .Returns((false, null));
        var context = BuildContext(policy);

        await BuildFilter(BuildSettings(create: 7, admin: 11, vote: 13))
            .OnActionExecutionAsync(context, Next(new[] { false }, context));

        capturedKey.ShouldStartWith(keyPrefix);
        capturedKey.ShouldContain(userId.ToString());
        capturedMax.ShouldBe(expectedMax);
        capturedWindow.ShouldBe(1);
    }

    [Fact]
    public async Task OnActionExecutionAsync_PublicRead_UsesIpKeyRegardlessOfAuth()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(Guid.NewGuid());
        string? capturedKey = null;
        int capturedMax = 0;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Do<int>(m => capturedMax = m), Arg.Any<int>())
            .Returns((false, null));
        var context = BuildContext(ReviewRateLimitPolicy.PublicRead, ip: "9.9.9.9");

        await BuildFilter(BuildSettings(reads: 61)).OnActionExecutionAsync(context, Next(new[] { false }, context));

        capturedKey.ShouldBe("review_read_ip_9.9.9.9");
        capturedMax.ShouldBe(61);
    }

    [Fact]
    public async Task OnActionExecutionAsync_AnonymousCreateReview_UsesIpSegment()
    {
        _currentUser.IsAuthenticated.Returns(false);
        string? capturedKey = null;
        _rateLimit.IsLimitedAsync(Arg.Do<string>(k => capturedKey = k), Arg.Any<int>(), Arg.Any<int>())
            .Returns((false, null));
        var context = BuildContext(ReviewRateLimitPolicy.CreateReview, ip: "2.2.2.2");

        await BuildFilter().OnActionExecutionAsync(context, Next(new[] { false }, context));

        capturedKey.ShouldBe("review_create_ip_2.2.2.2");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenLimited_Returns429WithRetryAfter()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _rateLimit.IsLimitedAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns((true, TimeSpan.FromSeconds(30)));
        var context = BuildContext(ReviewRateLimitPolicy.Vote);
        var called = new[] { false };

        await BuildFilter().OnActionExecutionAsync(context, Next(called, context));

        called[0].ShouldBeFalse();
        var obj = context.Result.ShouldBeOfType<ObjectResult>();
        obj.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        context.HttpContext.Response.Headers["Retry-After"].ToString().ShouldBe("00:00:30");
    }

    [Fact]
    public void ReviewRateLimitAttribute_ExposesPolicy()
    {
        new ReviewRateLimitAttribute(ReviewRateLimitPolicy.AdminAction).Policy
            .ShouldBe(ReviewRateLimitPolicy.AdminAction);
    }
}
