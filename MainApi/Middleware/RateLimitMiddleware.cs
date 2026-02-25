namespace MainApi.Middleware;

public class RateLimitMiddleware
{
    private const int AnonymousMaxAttempts = 100;
    private const int AuthenticatedMaxAttempts = 200;
    private const int WindowMinutes = 1;

    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public RateLimitMiddleware(
        RequestDelegate next,
        ILogger<RateLimitMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

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
            await WriteLimitExceededResponseAsync(context, key, retryAfter);
            return;
        }

        await _next(context);
    }

    private static (string Key, int MaxAttempts) ResolveRateLimitPolicy(
        ICurrentUserService currentUserService,
        string clientIp)
    {
        if (currentUserService.UserId.HasValue)
            return ($"global_user_{currentUserService.UserId.Value}", AuthenticatedMaxAttempts);

        return ($"global_anon_{clientIp}", AnonymousMaxAttempts);
    }

    private async Task WriteLimitExceededResponseAsync(
        HttpContext context,
        string key,
        int retryAfter)
    {
        _logger.LogWarning("Global rate limit exceeded for {Key}", key);
        context.Response.Headers.Append("Retry-After", retryAfter.ToString());
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
    }
}