using Application.Security.Contracts;
using Presentation.Common.Extensions;

namespace Presentation.Common.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class OtpRateLimitAttribute : System.Attribute
{ }

public sealed class OtpRateLimitFilter(
    IRateLimitService rateLimitService,
    ILogger<OtpRateLimitFilter> logger) : IAsyncActionFilter
{
    private const int MaxAttempts = 5;
    private const int WindowMinutes = 10;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hasAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<OtpRateLimitAttribute>()
            .Any();

        if (!hasAttribute)
        {
            await next();
            return;
        }

        var ip = HttpContextHelper.GetClientIpAddress(context.HttpContext);
        var key = $"otp_rl_{ip}";

        var (isLimited, retryAfter) = await rateLimitService.IsLimitedAsync(key, MaxAttempts, WindowMinutes);

        if (isLimited)
        {
            logger.LogWarning("OTP rate limit exceeded for IP {Ip}", ip);
            context.HttpContext.Response.Headers.Append("Retry-After", retryAfter.ToString());
            context.Result = new ObjectResult(new { message = "تعداد درخواست‌های OTP بیش از حد مجاز است." })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
            return;
        }

        await next();
    }
}