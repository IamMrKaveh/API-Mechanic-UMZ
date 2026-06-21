using Application.Common.Interfaces;
using Serilog.Context;

namespace Presentation.Common.Middleware;

public sealed class RequestLoggingEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
    {
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("UserId", currentUser.UserId))
        using (LogContext.PushProperty("IpAddress", currentUser.IpAddress))
        using (LogContext.PushProperty("UserAgent", currentUser.UserAgent))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            await next(context);
        }
    }
}