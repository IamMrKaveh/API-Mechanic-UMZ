namespace MainApi.Middlewares
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecuritySettings _securitySettings;
        private readonly ILogger<SecurityMiddleware> _logger;

        public SecurityMiddleware(RequestDelegate next, IOptions<SecuritySettings> securitySettings, ILogger<SecurityMiddleware> logger)
        {
            _next = next;
            _securitySettings = securitySettings.Value;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/admin"))
            {
                var remoteIp = context.Connection.RemoteIpAddress;
                var whitelisted = false;

                // Check if the IP is whitelisted (if the list is not empty)
                if (_securitySettings.AdminIpWhitelist != null && _securitySettings.AdminIpWhitelist.Any())
                {
                    if (remoteIp != null)
                    {
                        if (IPAddress.IsLoopback(remoteIp))
                        {
                            whitelisted = true;
                        }
                        else
                        {
                            whitelisted = _securitySettings.AdminIpWhitelist.Contains(remoteIp.ToString());
                        }
                    }
                }
                else
                {
                    // If the whitelist is empty, allow all IPs
                    whitelisted = true;
                }

                if (!whitelisted)
                {
                    _logger.LogWarning("Forbidden request to admin area from IP: {RemoteIp}", remoteIp);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access denied.");
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseAdminIpWhitelist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityMiddleware>();
        }
    }

    public class SecuritySettings
    {
        public List<string> AdminIpWhitelist { get; set; } = [];
    }
}