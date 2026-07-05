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

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        var entry = new Dictionary<string, object?>
        {
            ["level"] = LevelCode(logEvent.Level),
            ["message"] = logEvent.RenderMessage(CultureInfo.InvariantCulture)
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

            var converted = ToJsonValue(value);
            if (converted is null || (converted is string s && string.IsNullOrEmpty(s)))
                continue;

            entry[ToCamelCase(key)] = converted;
        }

        if (logEvent.Exception is { } exception)
        {
            entry["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name;
            entry["exceptionMessage"] = exception.Message;

            if (exception.InnerException is { } inner)
            {
                entry["innerExceptionType"] = inner.GetType().FullName ?? inner.GetType().Name;
                entry["innerExceptionMessage"] = inner.Message;
            }

            if (exception.Data.Count > 0)
            {
                entry["exceptionData"] = exception.Data
                    .Cast<DictionaryEntry>()
                    .ToDictionary(e => e.Key.ToString() ?? string.Empty, e => e.Value?.ToString());
            }

            entry["exception"] = exception.ToString();
        }

        output.WriteLine(JsonSerializer.Serialize(entry, SerializerOptions));
    }

    private static string LevelCode(LogEventLevel level) => level switch
    {
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => level.ToString().ToUpperInvariant()[..Math.Min(3, level.ToString().Length)]
    };

    private static string? Simplify(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: null } => null,
        ScalarValue { Value: IFormattable formattable } => formattable.ToString(null, CultureInfo.InvariantCulture),
        ScalarValue scalar => scalar.Value?.ToString(),
        _ => value.ToString()
    };

    private static object? ToJsonValue(LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: null } => null,
        ScalarValue { Value: IFormattable formattable } => formattable.ToString(null, CultureInfo.InvariantCulture),
        ScalarValue scalar => scalar.Value,
        SequenceValue sequence => sequence.Elements.Select(ToJsonValue).ToList(),
        DictionaryValue dictionary => dictionary.Elements.ToDictionary(
            kvp => Simplify(kvp.Key) ?? string.Empty,
            kvp => ToJsonValue(kvp.Value)),
        StructureValue structure => structure.Properties.ToDictionary(
            p => ToCamelCase(p.Name),
            p => ToJsonValue(p.Value)),
        _ => value.ToString()
    };

    private static string ToCamelCase(string key)
        => string.IsNullOrEmpty(key) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}