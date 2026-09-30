using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedKernel.Results;
using JsonConverter = Newtonsoft.Json.JsonConverter;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Infrastructure.Cache.Serialization;

/// <summary>
/// Lets <see cref="ServiceResult{T}"/> round-trip through the Newtonsoft-based MediatR output-cache
/// Redis provider. The type only has private constructors and read-only members, so the default
/// serializer cannot rebuild it from JSON.
/// </summary>
public sealed class ServiceResultJsonConverter : JsonConverter
{
    private const BindingFlags FactoryFlags =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public override bool CanConvert(Type objectType)
        => objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ServiceResult<>);

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        var result = (ServiceResult)value;
        var valueType = value.GetType().GetGenericArguments()[0];

        writer.WriteStartObject();
        writer.WritePropertyName("isSuccess");
        writer.WriteValue(result.IsSuccess);

        if (result.IsSuccess)
        {
            var inner = value.GetType().GetProperty(nameof(ServiceResult<object>.ValueOrDefault))!.GetValue(value);
            writer.WritePropertyName("value");
            serializer.Serialize(writer, inner, valueType);
        }
        else
        {
            writer.WritePropertyName("error");
            serializer.Serialize(writer, result.Error, typeof(Error));
        }

        writer.WriteEndObject();
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var json = JObject.Load(reader);
        var valueType = objectType.GetGenericArguments()[0];

        if (json["isSuccess"]?.Value<bool>() == true)
        {
            var token = json["value"];
            var inner = token is null || token.Type == JTokenType.Null
                ? null
                : token.ToObject(valueType, serializer);

            var success = objectType.GetMethod(nameof(ServiceResult<object>.Success), FactoryFlags, [valueType])!;
            return success.Invoke(null, [inner]);
        }

        var error = json["error"]?.ToObject<Error>(serializer)
            ?? Error.Unexpected("Cached failure result could not be restored.");

        var failure = objectType.GetMethod(nameof(ServiceResult<object>.Failure), FactoryFlags, [typeof(Error)])!;
        return failure.Invoke(null, [error]);
    }
}
