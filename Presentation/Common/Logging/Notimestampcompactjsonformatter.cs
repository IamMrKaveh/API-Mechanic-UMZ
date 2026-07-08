using Serilog.Formatting;
using System.Collections;
using System.Globalization;
using System.Text.Encodings.Web;

namespace Presentation.Common.Logging;

public sealed class NoTimestampCompactJsonFormatter : ITextFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.Ordinal)
    {
        "SourceContext", "ActionId", "ActionName", "RequestId", "ConnectionId", "EventId", "MachineName"
    };

    private const int MaxDepth = 6;
    private const int MaxCollectionItems = 50;
    private const int MaxStringLength = 4000;

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        var entry = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["level"] = LevelCode(logEvent.Level),
            ["message"] = SafeRenderMessage(logEvent)
        };

        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue))
        {
            var source = Simplify(sourceContextValue);
            if (!string.IsNullOrEmpty(source))
            {
                var lastDot = source.LastIndexOf('.');
                entry["source"] = lastDot >= 0 ? source[(lastDot + 1)..] : source;
            }
        }

        foreach (var (key, value) in logEvent.Properties)
        {
            if (ExcludedProperties.Contains(key))
                continue;

            var converted = ToJsonValue(value, 0);
            if (converted is null || (converted is string s && string.IsNullOrEmpty(s)))
                continue;

            entry[ToCamelCase(key)] = converted;
        }

        if (logEvent.Exception is { } exception)
        {
            entry["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
            entry["exceptionMessage"] = Truncate(exception.Message);

            if (exception.InnerException is { } inner)
            {
                entry["innerExceptionType"] = inner.GetType().FullName ?? inner.GetType().Name;
                entry["innerExceptionMessage"] = Truncate(inner.Message);
            }

            if (exception.Data.Count > 0)
            {
                entry["exceptionData"] = exception.Data
                    .Cast<DictionaryEntry>()
                    .Take(MaxCollectionItems)
                    .ToDictionary(
                        e => e.Key.ToString() ?? string.Empty,
                        e => (object?)Truncate(e.Value?.ToString()));
            }

            entry["exception"] = Truncate(exception.ToString());
        }

        string serialized;
        try
        {
            serialized = JsonSerializer.Serialize(entry, SerializerOptions);
        }
        catch (Exception ex)
        {
            var fallback = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["level"] = LevelCode(logEvent.Level),
                ["message"] = SafeRenderMessage(logEvent),
                ["logFormatterError"] = ex.GetType().Name + ": " + ex.Message
            };
            serialized = JsonSerializer.Serialize(fallback, SerializerOptions);
        }

        output.WriteLine(serialized);
    }

    private static string SafeRenderMessage(LogEvent logEvent)
    {
        try
        {
            return logEvent.RenderMessage(CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            return $"[RenderMessageFailed:{ex.GetType().Name}] {logEvent.MessageTemplate.Text}";
        }
    }

    private static string LevelCode(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => level.ToString().ToUpperInvariant()
    };

    private static string? Simplify(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: null } => null,
        ScalarValue { Value: IFormattable formattable } => formattable.ToString(null, CultureInfo.InvariantCulture),
        ScalarValue scalar => scalar.Value?.ToString(),
        _ => value.ToString()
    };

    private static object? ToJsonValue(LogEventPropertyValue value, int depth)
    {
        if (depth > MaxDepth)
            return "[MaxDepthExceeded]";

        return value switch
        {
            ScalarValue { Value: null } => null,
            ScalarValue scalar => ConvertScalar(scalar.Value),
            SequenceValue sequence => sequence.Elements
                .Take(MaxCollectionItems)
                .Select(e => ToJsonValue(e, depth + 1))
                .ToList(),
            DictionaryValue dictionary => dictionary.Elements
                .Take(MaxCollectionItems)
                .ToDictionary(
                    kvp => Simplify(kvp.Key) ?? string.Empty,
                    kvp => ToJsonValue(kvp.Value, depth + 1)),
            StructureValue structure => structure.Properties
                .Take(MaxCollectionItems)
                .ToDictionary(
                    p => ToCamelCase(p.Name),
                    p => ToJsonValue(p.Value, depth + 1)),
            _ => Truncate(value.ToString())
        };
    }

    private static object? ConvertScalar(object? raw)
    {
        if (raw is null) return null;

        switch (raw)
        {
            case string s: return Truncate(s);
            case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                return raw;

            case Guid g: return g.ToString();
            case DateTime dt: return dt.ToString("O", CultureInfo.InvariantCulture);
            case DateTimeOffset dto: return dto.ToString("O", CultureInfo.InvariantCulture);
            case TimeSpan ts: return ts.ToString(null, CultureInfo.InvariantCulture);
            case Uri uri: return uri.ToString();
            case Enum e: return e.ToString();
            case IFormattable f: return f.ToString(null, CultureInfo.InvariantCulture);
            default:
                return Truncate(SafeToString(raw));
        }
    }

    private static string SafeToString(object obj)
    {
        try
        {
            var type = obj.GetType();
            if (type.Namespace?.StartsWith("System.Reflection", StringComparison.Ordinal) == true
                || type.Namespace?.StartsWith("System.RuntimeType", StringComparison.Ordinal) == true)
            {
                return $"[{type.Name}]";
            }
            return obj.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            return $"[ToStringFailed:{ex.GetType().Name}]";
        }
    }

    private static string? Truncate(string? value)
    {
        if (value is null) return null;
        return value.Length <= MaxStringLength ? value : value[..MaxStringLength] + "…[truncated]";
    }

    private static string ToCamelCase(string key)
        => string.IsNullOrEmpty(key) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}