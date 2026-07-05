using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Presentation.Common.Extensions;

public static class SecurityExtensions
{
    private const long SlowRequestThresholdMs = 2000;

    private static readonly HashSet<int> SuppressedStatusCodes = new()
    {
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound
    };

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
    {
        if (elapsedMs > SlowRequestThresholdMs)
        {
            return true;
        }

        var status = context.Response.StatusCode;
        if (status < 400)
        {
            return false;
        }

        return !SuppressedStatusCodes.Contains(status);
    }

    private static void LogSlowOrFailedRequest(HttpContext context, long elapsedMs)
    {
        Log.ForContext("RequestPath", context.Request.Path)
           .ForContext("RequestMethod", context.Request.Method)
           .ForContext("ResponseStatusCode", context.Response.StatusCode)
           .ForContext("ElapsedMilliseconds", elapsedMs)
           .Warning("Slow or failed request detected.");
    }
}