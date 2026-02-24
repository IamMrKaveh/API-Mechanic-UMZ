namespace MainApi.Middleware;

public class WebhookIpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebhookIpWhitelistMiddleware> _logger;
    private readonly string[] _allowedIps;

    public WebhookIpWhitelistMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<WebhookIpWhitelistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _allowedIps = configuration.GetSection("Zarinpal:AllowedIps").Get<string[]>() ?? Array.Empty<string>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/payments/webhook/zarinpal"))
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (remoteIp == null || !_allowedIps.Contains(remoteIp))
            {
                _logger.LogWarning("Unauthorized Webhook attempt from IP: {RemoteIp}", remoteIp);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }
        await _next(context);
    }
}