namespace MainApi.Services.RateLimit;

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
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(clientIp))
        {
            await _next(context);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var rateLimitService = scope.ServiceProvider.GetRequiredService<IRateLimitService>();

        var key = $"global_{clientIp}";

        if (await rateLimitService.IsLimitedAsync(key, 100, 1))
        {
            _logger.LogWarning("Global rate limit exceeded for IP: {ClientIP}", clientIp);
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        await _next(context);
    }
}