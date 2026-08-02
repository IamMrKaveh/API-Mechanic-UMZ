using Application.Common.Interfaces;
using Application.Review.Configuration;
using Application.Security.Contracts;

namespace Presentation.Common.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ReviewRateLimitAttribute : System.Attribute
{
    public ReviewRateLimitPolicy Policy { get; }

    public ReviewRateLimitAttribute(ReviewRateLimitPolicy policy) => Policy = policy;
}

public enum ReviewRateLimitPolicy
{
    CreateReview,
    PublicRead,
    AdminAction,
    Vote
}

public sealed class ReviewRateLimitFilter(
    IRateLimitService rateLimitService,
    ICurrentUserService currentUserService,
    IOptions<ReviewSettings> settings,
    ILogger<ReviewRateLimitFilter> logger) : IAsyncActionFilter
{
    private const int WindowMinutes = 1;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<ReviewRateLimitAttribute>()
            .FirstOrDefault();

        if (attribute is null)
        {
            await next();
            return;
        }

        var (key, maxAttempts) = ResolvePolicy(attribute.Policy, context.HttpContext);

        var (isLimited, retryAfter) =
            await rateLimitService.IsLimitedAsync(key, maxAttempts, WindowMinutes);

        if (isLimited)
        {
            logger.LogWarning(
                "Review rate limit exceeded for policy {Policy} key {Key}",
                attribute.Policy, key);

            context.HttpContext.Response.Headers.Append("Retry-After", retryAfter.ToString());
            context.Result = new ObjectResult(new
            {
                success = false,
                message = "تعداد درخواست‌ها بیش از حد مجاز است. لطفاً بعداً تلاش کنید.",
                errors = new { rateLimit = new[] { "Rate limit exceeded." } }
            })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
            return;
        }

        await next();
    }

    private (string Key, int MaxAttempts) ResolvePolicy(
        ReviewRateLimitPolicy policy, HttpContext http)
    {
        var rate = settings.Value.RateLimit;
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userSegment = currentUserService.IsAuthenticated
            ? $"user_{currentUserService.UserId!.Value}"
            : $"ip_{ip}";

        return policy switch
        {
            ReviewRateLimitPolicy.CreateReview =>
                ($"review_create_{userSegment}", rate.CreateReviewPerMinute),
            ReviewRateLimitPolicy.PublicRead =>
                ($"review_read_ip_{ip}", rate.PublicReadsPerMinute),
            ReviewRateLimitPolicy.AdminAction =>
                ($"review_admin_{userSegment}", rate.AdminActionsPerMinute),
            ReviewRateLimitPolicy.Vote =>
                ($"review_vote_{userSegment}", rate.VotePerMinute),
            _ => ($"review_default_{userSegment}", rate.PublicReadsPerMinute)
        };
    }
}
