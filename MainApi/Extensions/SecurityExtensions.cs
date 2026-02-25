namespace MainApi.Extensions;

public static class SecurityExtensions
{
    private const long SlowRequestThresholdMs = 2000;

    public static IApplicationBuilder UseRequestPerformanceMonitoring(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var sw = Stopwatch.StartNew();
            await next.Invoke();
            sw.Stop();

            if (ShouldLogRequest(context, sw.ElapsedMilliseconds))
            {
                LogSlowOrFailedRequest(context, sw.ElapsedMilliseconds);
            }
        });

        return app;
    }

    private static bool ShouldLogRequest(HttpContext context, long elapsedMs)
        => elapsedMs > SlowRequestThresholdMs ||
           (context.Response.StatusCode >= 400 && context.Response.StatusCode != 404);

    private static void LogSlowOrFailedRequest(HttpContext context, long elapsedMs)
    {
        Log.ForContext("RequestPath", context.Request.Path)
           .ForContext("RequestMethod", context.Request.Method)
           .ForContext("ResponseStatusCode", context.Response.StatusCode)
           .ForContext("ElapsedMilliseconds", elapsedMs)
           .Warning("Slow or failed request detected.");
    }
}