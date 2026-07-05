namespace Presentation.Common.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private const string SourceName = "RequestLoggingMiddleware";

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? capturedException = null;

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            WriteLog(context, stopwatch.Elapsed.TotalMilliseconds, capturedException);
        }
    }

    private void WriteLog(HttpContext context, double elapsedMs, Exception? exception)
    {
        var request = context.Request;
        var statusCode = context.Response?.StatusCode ?? 0;
        var level = ResolveLogLevel(statusCode, exception);

        if (!logger.IsEnabled(level))
        {
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "-";
        var userAgent = request.Headers.UserAgent.ToString();
        var traceId = context.TraceIdentifier;

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["source"] = SourceName,
            ["remoteIpAddress"] = remoteIp,
            ["userAgent"] = userAgent,
            ["requestMethod"] = request.Method,
            ["requestPath"] = request.Path.Value,
            ["statusCode"] = statusCode,
            ["elapsed"] = elapsedMs,
            ["ipAddress"] = remoteIp,
            ["traceId"] = traceId,
        }))
        {
            if (exception is null)
            {
                logger.Log(
                    level,
                    "HTTP \"{RequestMethod}\" \"{RequestPath}\" responded {StatusCode} in {Elapsed:F4} ms",
                    request.Method,
                    request.Path.Value,
                    statusCode,
                    elapsedMs);
            }
            else
            {
                logger.Log(
                    level,
                    exception,
                    "HTTP \"{RequestMethod}\" \"{RequestPath}\" failed with {StatusCode} in {Elapsed:F4} ms",
                    request.Method,
                    request.Path.Value,
                    statusCode,
                    elapsedMs);
            }
        }
    }

    private static LogLevel ResolveLogLevel(int statusCode, Exception? exception)
    {
        if (exception is not null)
        {
            return LogLevel.Error;
        }

        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information,
        };
    }
}