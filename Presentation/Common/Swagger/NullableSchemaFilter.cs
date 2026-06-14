using System.Reflection;

namespace Presentation.Common.Swagger;

public sealed class NullableSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema?.Properties is null || schema.Properties.Count == 0)
            return;

        if (context.Type is null)
            return;

        var nullableProperties = context.Type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsNullable)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (nullableProperties.Count == 0)
            return;

        foreach (var schemaKey in schema.Properties.Keys.ToList())
        {
            if (!nullableProperties.Contains(schemaKey))
                continue;

            schema.Properties[schemaKey].Nullable = true;
            schema.Required.Remove(schemaKey);
        }
    }

    private static bool IsNullable(PropertyInfo property)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
            return true;

        if (property.PropertyType.IsValueType)
            return false;

        var info = NullabilityContext.Create(property);
        return info.WriteState == NullabilityState.Nullable
            || info.ReadState == NullabilityState.Nullable;
    }
}