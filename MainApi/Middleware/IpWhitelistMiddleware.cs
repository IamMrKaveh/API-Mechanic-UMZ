namespace MainApi.Middleware;

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpWhitelistMiddleware> _logger;
    private readonly string[] _whitelist;

    public IpWhitelistMiddleware(RequestDelegate next, ILogger<IpWhitelistMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _whitelist = configuration.GetSection("Security:AdminIpWhitelist").Get<string[]>() ?? Array.Empty<string>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/Admin") || context.Request.Path.StartsWithSegments("/swagger"))
        {
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp == null)
            {
                _logger.LogWarning("IP whitelist check failed: Remote IP address is null.");
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            var ipString = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4().ToString() : remoteIp.ToString();

            if (!_whitelist.Contains(ipString))
            {
                _logger.LogWarning("Forbidden request from IP: {RemoteIp}", ipString);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }
        }

        await _next(context);
    }
}
