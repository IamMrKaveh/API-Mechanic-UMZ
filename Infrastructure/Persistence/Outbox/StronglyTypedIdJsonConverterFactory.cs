using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.ValueObjects;

namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// Serializes every <see cref="IStronglyTypedId"/> as its underlying <see cref="Guid"/>
/// and rebuilds it through the <c>From(Guid)</c> factory of the identifier itself.
/// </summary>
public sealed class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => !typeToConvert.IsAbstract
           && !typeToConvert.IsInterface
           && typeof(IStronglyTypedId).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(
            typeof(StronglyTypedIdJsonConverter<>).MakeGenericType(typeToConvert))!;
}

public sealed class StronglyTypedIdJsonConverter<TId> : JsonConverter<TId>
    where TId : class, IStronglyTypedId
{
    private static readonly MethodInfo FromMethod = FindFromMethod(typeof(TId));

    private static MethodInfo FindFromMethod(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var method = current.GetMethod(
                "From",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Guid)],
                modifiers: null);

            if (method is not null)
                return method;
        }

        throw new InvalidOperationException(
            $"'{typeof(TId).Name}' must expose a public static 'From(Guid)' factory to be used in an outbox payload.");
    }

    public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null!;

        var guid = reader.GetGuid();
        return (TId)FromMethod.Invoke(null, [guid])!;
    }

    public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value);
    }
}
