namespace MainApi.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Response.Headers.Append(HeaderName, correlationId);

        using (_logger.BeginScope("CorrelationId: {CorrelationId}", correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
        => context.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString();
}