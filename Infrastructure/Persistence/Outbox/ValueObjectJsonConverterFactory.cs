using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Common.Abstractions;
using SharedKernel.Abstractions;
using SharedKernel.ValueObjects;

namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// Serializes the immutable value objects carried by domain events as an object of
/// their primitive components and rebuilds them through their private constructor
/// or their canonical static factory.
///
/// The set of supported types is discovered from the domain events themselves, so a
/// new event that carries a new value object is covered automatically.
/// </summary>
public sealed class ValueObjectJsonConverterFactory : JsonConverterFactory
{
    private static readonly Lazy<HashSet<Type>> ValueCarrierTypes = new(DiscoverValueCarrierTypes);

    public override bool CanConvert(Type typeToConvert)
        => ValueCarrierTypes.Value.Contains(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(
            typeof(ValueObjectJsonConverter<>).MakeGenericType(typeToConvert))!;

    private static HashSet<Type> DiscoverValueCarrierTypes()
    {
        var types = new HashSet<Type>();
        var domainAssembly = typeof(IDomainEvent).Assembly;

        foreach (var eventType in domainAssembly.GetTypes())
        {
            if (eventType.IsAbstract || !typeof(IDomainEvent).IsAssignableFrom(eventType))
                continue;

            foreach (var constructor in eventType.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    var parameterType = parameter.ParameterType;

                    if (RequiresCustomConverter(parameterType))
                        types.Add(parameterType);
                }
            }
        }

        return types;
    }

    private static bool RequiresCustomConverter(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericType)
            return false;

        if (type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(Guid) || type == typeof(DateTime)
            || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
        {
            return false;
        }

        if (type.IsValueType)
            return false;

        if (typeof(IStronglyTypedId).IsAssignableFrom(type))
            return false;

        if (typeof(ValueObject).IsAssignableFrom(type))
            return true;

        var hasPublicParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) is not null;
        if (hasPublicParameterlessConstructor)
            return false;

        return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Length == 0;
    }
}

public sealed class ValueObjectJsonConverter<T> : JsonConverter<T>
    where T : class
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null!;

        using var document = JsonDocument.ParseValue(ref reader);

        return ValueObjectJsonReader.Create<T>(document.RootElement, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        foreach (var property in ValueObjectJsonReader.GetComponentProperties(typeof(T)))
        {
            writer.WritePropertyName(
                options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name);

            JsonSerializer.Serialize(writer, property.GetValue(value), options);
        }

        writer.WriteEndObject();
    }
}

internal static class ValueObjectJsonReader
{
    private static readonly string[] FactoryMethodNames = ["Create", "FromString", "From", "Of"];

    public static IReadOnlyList<PropertyInfo> GetComponentProperties(Type type)
    {
        var properties = new List<PropertyInfo>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead)
                continue;

            if (property.GetIndexParameters().Length > 0)
                continue;

            if (!IsComponentType(property.PropertyType))
                continue;

            properties.Add(property);
        }

        return properties;
    }

    public static T Create<T>(JsonElement root, JsonSerializerOptions options)
        where T : class
    {
        var type = typeof(T);
        var properties = GetComponentProperties(type);

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in properties)
        {
            if (!TryGetProperty(root, property.Name, out var element))
                continue;

            values[property.Name] = element.Deserialize(property.PropertyType, options);
        }

        var instance = CreateFromConstructor(type, values)
            ?? CreateFromFactory(type, values);

        return instance is null
            ? throw new JsonException(
                $"Unable to rebuild the value object '{type.FullName}' from its serialized components.")
            : (T)instance;
    }

    private static object? CreateFromConstructor(Type type, Dictionary<string, object?> values)
    {
        var constructors = type
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(ctor => ctor.GetParameters().Length > 0)
            .OrderByDescending(ctor => ctor.GetParameters().Length);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var arguments = new object?[parameters.Length];
            var matched = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var name = parameters[i].Name;

                if (name is null || !values.TryGetValue(name, out var value))
                {
                    matched = false;
                    break;
                }

                arguments[i] = value;
            }

            if (!matched)
                continue;

            return constructor.Invoke(arguments);
        }

        return null;
    }

    private static object? CreateFromFactory(Type type, Dictionary<string, object?> values)
    {
        if (!values.TryGetValue("Value", out var component) || component is null)
            return null;

        if (values.Count != 1)
            return null;

        foreach (var name in FactoryMethodNames)
        {
            var factory = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                    method.Name == name
                    && method.GetParameters().Length == 1
                    && method.GetParameters()[0].ParameterType.IsInstanceOfType(component));

            if (factory is not null)
                return factory.Invoke(null, [component]);
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement root, string name, out JsonElement value)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty(name, out value))
                return true;

            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool IsComponentType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying.IsEnum)
            return true;

        return Type.GetTypeCode(underlying) switch
        {
            TypeCode.Boolean => true,
            TypeCode.Byte => true,
            TypeCode.SByte => true,
            TypeCode.Int16 => true,
            TypeCode.UInt16 => true,
            TypeCode.Int32 => true,
            TypeCode.UInt32 => true,
            TypeCode.Int64 => true,
            TypeCode.UInt64 => true,
            TypeCode.Single => true,
            TypeCode.Double => true,
            TypeCode.Decimal => true,
            TypeCode.Char => true,
            TypeCode.String => true,
            _ => underlying == typeof(TimeSpan)
                || underlying == typeof(DateTimeOffset)
                || underlying == typeof(Guid)
        };
    }
}
