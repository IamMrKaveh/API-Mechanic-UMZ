using Application.Common.Interfaces;
using Application.Security.Contracts;

namespace Presentation.Common.Middleware;

public class RateLimitMiddleware(
    RequestDelegate next,
    ILogger<RateLimitMiddleware> logger,
    IServiceScopeFactory scopeFactory)
{
    private const int AnonymousMaxAttempts = 100;
    private const int UnknownIpMaxAttempts = 30;
    private const int AuthenticatedMaxAttempts = 200;
    private const int WindowMinutes = 1;

    private readonly RequestDelegate _next = next;
    private readonly ILogger<RateLimitMiddleware> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public async Task InvokeAsync(HttpContext context)
    {
        // A missing client IP must not bypass limiting: such callers share a
        // strict "unknown" bucket instead of being waved through.
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

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
            return ($"rl_user_{currentUserService.UserId!.Value}", AuthenticatedMaxAttempts);

        var (key, limit) = ($"rl_ip_{clientIp}", AnonymousMaxAttempts);

        // Tighten the shared bucket for callers we cannot identify.
        return clientIp == "unknown"
            ? ($"rl_ip_unknown", UnknownIpMaxAttempts)
            : (key, limit);
    }
}
