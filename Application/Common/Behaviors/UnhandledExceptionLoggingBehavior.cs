using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Application.Common.Behaviors;

public sealed class UnhandledExceptionLoggingBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionLoggingBehavior<TRequest, TResponse>> logger,
    IAuditMaskingService maskingService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Request {RequestName} canceled after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(
                ex,
                "Unhandled exception in {RequestName} after {ElapsedMilliseconds} ms. Payload: {Payload}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                SafeSerializeAndMask(request));

            throw;
        }
    }

    private string SafeSerializeAndMask(TRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, SerializerOptions);
            return maskingService.MaskSensitiveData(json);
        }
        catch
        {
            return "[serialization-failed]";
        }
    }
}