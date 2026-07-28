using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        MaxDepth = 6
    };

    private static readonly string[] SensitiveKeys =
    {
        "password", "token", "secret", "authorization", "apikey", "creditcard", "cvv"
    };

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommand && request is not ICommand<Unit> && !IsCommand(request))
            return await next(cancellationToken);

        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var payload = SerializePayload(request);

        logger.LogInformation(
            "Handling command {CommandName} with payload {Payload}",
            requestName,
            payload);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();
            logger.LogInformation(
                "Handled command {CommandName} in {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (ValidationException validationException)
        {
            stopwatch.Stop();
            var validationErrors = validationException.Errors
                .Select(e => new { Property = e.PropertyName, Message = e.ErrorMessage, Value = e.AttemptedValue })
                .ToArray();

            logger.LogWarning(
                "Validation failed for command {CommandName} after {ElapsedMilliseconds} ms. Payload={Payload} Errors={Errors}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                payload,
                JsonSerializer.Serialize(validationErrors, SerializerOptions));

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Command {CommandName} failed after {ElapsedMilliseconds} ms. Payload={Payload}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                payload);
            throw;
        }
    }

    private static bool IsCommand(TRequest request)
    {
        var type = request.GetType();
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("ICommand", StringComparison.Ordinal));
    }

    private static string SerializePayload(TRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, SerializerOptions);
            return MaskSensitiveValues(json);
        }
        catch (Exception ex)
        {
            return $"<unserializable: {ex.Message}>";
        }
    }

    private static string MaskSensitiveValues(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        var masked = json;
        foreach (var key in SensitiveKeys)
        {
            var pattern = $"\"{key}\"\\s*:\\s*\"[^\"]*\"";
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked,
                pattern,
                $"\"{key}\":\"***\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return masked;
    }
}
