using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.ValueObjects;

namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// Shared System.Text.Json configuration for the transactional outbox.
///
/// Domain events carry immutable value objects (strongly typed identifiers, money,
/// slugs, enum-like value objects). Those types intentionally expose no public
/// constructor, so the default serializer cannot rebuild them and would silently
/// materialize <c>null</c> instead. The converters registered here restore them from
/// their primitive components so that an outbox payload survives the write/read round
/// trip intact.
/// </summary>
public static class DomainEventJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        options.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        options.Converters.Add(new ValueObjectJsonConverterFactory());

        return options;
    }
}
