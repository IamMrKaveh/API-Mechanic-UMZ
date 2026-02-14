using Application.Security.Contracts;

namespace MainApi.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class PaymentRateLimitAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var rateLimitService = httpContext.RequestServices.GetRequiredService<IRateLimitService>();
        var user = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        var key = user.UserId.HasValue
            ? $"payment_limit_user_{user.UserId}"
            : $"payment_limit_ip_{httpContext.Connection.RemoteIpAddress}";

        // Limit: 3 attempts per 10 minutes
        var (isLimited, retryAfter) = await rateLimitService.IsLimitedAsync(key, 3, 10);

        if (isLimited)
        {
            context.Result = new ObjectResult(new { message = $"Too many payment attempts. Try again in {retryAfter} seconds." })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
            return;
        }

        await next();
    }
}