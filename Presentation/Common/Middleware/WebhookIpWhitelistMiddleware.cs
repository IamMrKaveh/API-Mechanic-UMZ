using Presentation.Common.Options;

namespace Presentation.Common.Middleware;

public class WebhookIpWhitelistMiddleware
{
    private const string AllowedIpsConfigSection = "Zarinpal:AllowedIps";

    private readonly RequestDelegate _next;
    private readonly ILogger<WebhookIpWhitelistMiddleware> _logger;
    private readonly string[] _allowedIps;
    private readonly WebhookOptions _webhookOptions;

    public WebhookIpWhitelistMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IOptions<WebhookOptions> webhookOptions,
        ILogger<WebhookIpWhitelistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _allowedIps = configuration.GetSection(AllowedIpsConfigSection).Get<string[]>() ?? [];
        _webhookOptions = webhookOptions.Value;
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

    private bool IsWebhookRequest(HttpContext context)
        => _webhookOptions.AllowedPaths.Any(path =>
            context.Request.Path.StartsWithSegments(path));

    private bool IsAllowedIp(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return remoteIp != null && _allowedIps.Contains(remoteIp);
    }
}