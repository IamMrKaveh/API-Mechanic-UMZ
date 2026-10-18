using Infrastructure.Security.Settings;

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
        if (IsAdminPath(context.Request.Path))
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

    /// <summary>
    /// Matches <c>/api/admin/...</c> and versioned <c>/api/v{version}/admin/...</c>
    /// routes. Admin controllers are mounted under <c>api/v{version}/admin</c>,
    /// so the unversioned prefix alone would never match.
    /// </summary>
    internal static bool IsAdminPath(PathString path)
    {
        if (!path.StartsWithSegments("/api", out var rest))
            return false;

        var segments = rest.Value!
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        // /api/admin/... or /api/v1/admin/...
        return segments.Length >= 2
               && string.Equals(segments[0], "admin", StringComparison.OrdinalIgnoreCase)
            || segments.Length >= 3
               && segments[0].StartsWith('v')
               && int.TryParse(segments[0].AsSpan(1), out _)
               && string.Equals(segments[1], "admin", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsWhitelisted(HttpContext context)
    {
        // Fail-closed: an explicitly configured-but-empty admin whitelist must
        // deny every admin request instead of silently allowing all traffic.
        if (_securitySettings.AdminIpWhitelist is not { Count: > 0 })
        {
            _logger.LogWarning(
                "Admin IP whitelist is empty; denying admin-area access. " +
                "Configure Security:AdminIpWhitelist or remove the section to disable the check.");
            return false;
        }

        var remoteIp = NormalizeRemoteIp(context.Connection.RemoteIpAddress);
        if (remoteIp is null)
            return false;

        if (IPAddress.IsLoopback(remoteIp))
            return true;

        return _securitySettings.AdminIpWhitelist.Contains(remoteIp.ToString());
    }

    /// <summary>
    /// Proxies may hand us IPv4 clients as IPv4-mapped IPv6 addresses
    /// (<c>::ffff:1.2.3.4</c>); normalize so whitelist entries written as
    /// plain IPv4 still match.
    /// </summary>
    private static IPAddress? NormalizeRemoteIp(IPAddress? remoteIp)
    {
        if (remoteIp is null)
            return null;

        return remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;
    }
}

public static class SecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminIpWhitelist(this IApplicationBuilder builder)
        => builder.UseMiddleware<SecurityMiddleware>();
}