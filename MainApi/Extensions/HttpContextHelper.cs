namespace MainApi.Extensions;

public static class HttpContextHelper
{
    public static string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return "unknown";
        }

        // Check for X-Forwarded-For header
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        // Fallback to connection's remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}