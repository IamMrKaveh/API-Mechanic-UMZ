namespace MainApi.Extensions;
/// 
/// Extension methods for configuring security services
/// 
public static class SecurityExtensions
{
    /// 
    /// Adds IP whitelist authorization policy for admin endpoints
    /// 
    public static IServiceCollection AddIpWhitelist(
    this IServiceCollection services,
    IConfiguration configuration)
    {
        var whitelistedIps = configuration.GetSection("Security:AdminIpWhitelist").Get<string[]>();
        if (whitelistedIps != null && whitelistedIps.Any())
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminIpWhitelist", policy =>
                {
                    policy.RequireRole("Admin");
                    policy.RequireAssertion(context =>
                    {
                        var httpContext = context.Resource as HttpContext;
                        if (httpContext == null) return false;

                        var ipString = httpContext.Connection.RemoteIpAddress?.ToString();

                        return !string.IsNullOrEmpty(ipString) && whitelistedIps.Contains(ipString);
                    });
                });
            });
        }
        return services;
    }
    /// 
    /// Adds security-related middleware to the application pipeline
    /// 
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
    {
        // Custom middleware for logging slow or failed requests
        app.Use(async (context, next) =>
        {
            var sw = Stopwatch.StartNew();
            await next.Invoke();
            sw.Stop();
            if (sw.ElapsedMilliseconds > 2000 ||
            (context.Response.StatusCode >= 400 && context.Response.StatusCode != 404))
            {
                Log.ForContext("RequestPath", context.Request.Path)
                .ForContext("RequestMethod", context.Request.Method)
                .ForContext("ResponseStatusCode", context.Response.StatusCode)
                .ForContext("ElapsedMilliseconds", sw.ElapsedMilliseconds)
                .Warning("Slow or failed request detected.");
            }
        });
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RateLimitMiddleware>();
        return app;
    }
}