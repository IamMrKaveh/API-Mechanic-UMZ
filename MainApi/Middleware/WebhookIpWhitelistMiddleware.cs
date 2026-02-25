namespace MainApi.Middleware;

public class WebhookIpWhitelistMiddleware
{
    private const string WebhookPath = "/api/payments/webhook/zarinpal";
    private const string AllowedIpsConfigSection = "Zarinpal:AllowedIps";

    private readonly RequestDelegate _next;
    private readonly ILogger<WebhookIpWhitelistMiddleware> _logger;
    private readonly string[] _allowedIps;

    public WebhookIpWhitelistMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<WebhookIpWhitelistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _allowedIps = configuration.GetSection(AllowedIpsConfigSection).Get<string[]>() ?? [];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsWebhookRequest(context) && !IsAllowedIp(context))
        {
            _logger.LogWarning(
                "Unauthorized Webhook attempt from IP: {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }

    private static bool IsWebhookRequest(HttpContext context)
        => context.Request.Path.StartsWithSegments(WebhookPath);

    private bool IsAllowedIp(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return remoteIp != null && _allowedIps.Contains(remoteIp);
    }
}