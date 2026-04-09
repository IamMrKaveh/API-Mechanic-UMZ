using Presentation.Security.Settings;

namespace Presentation.Common.Middleware;

public class SecurityMiddleware(
    RequestDelegate next,
    IOptions<SecuritySettings> securitySettings,
    ILogger<SecurityMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly SecuritySettings _securitySettings = securitySettings.Value;
    private readonly ILogger<SecurityMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/admin"))
        {
            if (!IsWhitelisted(context))
            {
                _logger.LogWarning(
                    "Forbidden request to admin area from IP: {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied.");
                return;
            }
        }

        await _next(context);
    }

    private bool IsWhitelisted(HttpContext context)
    {
        if (_securitySettings.AdminIpWhitelist is not { Count: > 0 })
            return true;

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp == null)
            return false;

        if (IPAddress.IsLoopback(remoteIp))
            return true;

        return _securitySettings.AdminIpWhitelist.Contains(remoteIp.ToString());
    }
}

public static class SecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminIpWhitelist(this IApplicationBuilder builder)
        => builder.UseMiddleware<SecurityMiddleware>();
}