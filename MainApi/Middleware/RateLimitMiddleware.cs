namespace MainApi.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, IServiceScopeFactory scopeFactory)
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

        var key = $"global_anon_{clientIp}";
        var maxAttempts = 100;
        var windowMinutes = 1;

        var userId = currentUserService.UserId;
        if (userId.HasValue)
        {
            key = $"global_user_{userId.Value}";
            maxAttempts = 200;
        }

        if (await rateLimitService.IsLimitedAsync(key, maxAttempts, windowMinutes))
        {
            _logger.LogWarning("Global rate limit exceeded for {Key}", key);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        await _next(context);
    }
}