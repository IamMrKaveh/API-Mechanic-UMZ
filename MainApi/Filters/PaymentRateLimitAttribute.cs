namespace MainApi.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class PaymentRateLimitAttribute : ActionFilterAttribute
{
    private const int MaxAttempts = 3;
    private const int WindowMinutes = 10;

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var rateLimitService = httpContext.RequestServices.GetRequiredService<IRateLimitService>();
        var currentUserService = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        var key = ResolveRateLimitKey(currentUserService, httpContext);
        var (isLimited, retryAfter) = await rateLimitService.IsLimitedAsync(key, MaxAttempts, WindowMinutes);

        if (isLimited)
        {
            context.Result = BuildTooManyRequestsResult(retryAfter);
            return;
        }

        await next();
    }

    private static string ResolveRateLimitKey(ICurrentUserService currentUserService, HttpContext httpContext)
        => currentUserService.UserId.HasValue
            ? $"payment_limit_user_{currentUserService.UserId}"
            : $"payment_limit_ip_{httpContext.Connection.RemoteIpAddress}";

    private static ObjectResult BuildTooManyRequestsResult(int retryAfter)
        => new(new { message = $"Too many payment attempts. Try again in {retryAfter} seconds." })
        {
            StatusCode = StatusCodes.Status429TooManyRequests
        };
}