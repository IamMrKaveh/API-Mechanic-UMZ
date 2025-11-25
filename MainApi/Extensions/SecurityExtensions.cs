namespace MainApi.Extensions;

public static class SecurityExtensions
{
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
    {
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