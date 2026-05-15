namespace Presentation.Common.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers.Append(HeaderName, correlationId);
        await next(context);
    }

    private static string ResolveCorrelationId(HttpContext context)
        => context.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString();
}