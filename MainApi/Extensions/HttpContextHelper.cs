namespace MainApi.Extensions;

public static class HttpContextHelper
{
    public static string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
        {
            return "unknown";
        }

        
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }
        }

        
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}