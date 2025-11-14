namespace MainApi.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.TraceIdentifier = correlationId;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        using (_logger.BeginScope("CorrelationId: {CorrelationId}", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}