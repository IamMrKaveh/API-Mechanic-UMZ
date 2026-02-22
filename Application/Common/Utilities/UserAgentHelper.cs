namespace Application.Common.Utilities;

public static class UserAgentHelper
{
    public static string GetDeviceInfo(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "دستگاه نامشخص";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone")) return "موبایل";
        if (ua.Contains("tablet") || ua.Contains("ipad")) return "تبلت";
        return "کامپیوتر";
    }

    public static string GetBrowserInfo(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "نامشخص";
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("chrome") && !ua.Contains("edge")) return "Chrome";
        if (ua.Contains("firefox")) return "Firefox";
        if (ua.Contains("safari") && !ua.Contains("chrome")) return "Safari";
        if (ua.Contains("edge")) return "Edge";
        return "نامشخص";
    }
}