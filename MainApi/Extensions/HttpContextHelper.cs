namespace MainApi.Extensions;

public static class HttpContextHelper
{
    private static readonly string[] ForwardedForHeaders = ["X-Forwarded-For", "X-Real-IP"];

    public static string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "unknown";

        foreach (var header in ForwardedForHeaders)
        {
            var ip = ExtractIpFromHeader(httpContext, header);
            if (ip != null)
                return ip;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string? ExtractIpFromHeader(HttpContext context, string headerName)
    {
        if (!context.Request.Headers.TryGetValue(headerName, out var headerValue))
            return null;

        var ip = headerValue.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        return string.IsNullOrEmpty(ip) ? null : ip;
    }
}