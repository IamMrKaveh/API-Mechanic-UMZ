using Application.Security.Contracts;
using Presentation.Common.Extensions;
using SharedKernel.Contracts;

namespace Presentation.Common.Middleware;

public class RateLimitMiddleware(
    RequestDelegate next,
    ILogger<RateLimitMiddleware> logger,
    IServiceScopeFactory scopeFactory)
{
    private const int AnonymousMaxAttempts = 100;
    private const int AuthenticatedMaxAttempts = 200;
    private const int WindowMinutes = 1;

    private readonly RequestDelegate _next = next;
    private readonly ILogger<RateLimitMiddleware> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(context);
        if (string.IsNullOrEmpty(clientIp))
        {
            await _next(context);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var rateLimitService = scope.ServiceProvider.GetRequiredService<IRateLimitService>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

        var (key, maxAttempts) = ResolveRateLimitPolicy(currentUserService, clientIp);

        var (isLimited, retryAfter) = await rateLimitService.IsLimitedAsync(key, maxAttempts, WindowMinutes);

        if (isLimited)
        {
            _logger.LogWarning("Rate limit exceeded for {Key}", key);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.Append("Retry-After", retryAfter.ToString());
            await context.Response.WriteAsync("Too many requests. Please try again later.");
            return;
        }

        await _next(context);
    }

    private static (string key, int maxAttempts) ResolveRateLimitPolicy(
        ICurrentUserService currentUserService,
        string clientIp)
    {
        if (currentUserService.IsAuthenticated)
            return ($"rl_user_{currentUserService.CurrentUser.UserId}", AuthenticatedMaxAttempts);

        return ($"rl_ip_{clientIp}", AnonymousMaxAttempts);
    }
}