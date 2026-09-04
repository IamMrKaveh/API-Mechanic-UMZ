using System.Linq.Expressions;
using System.Reflection;
using Domain.Attribute.ValueObjects;

namespace Tests.Infrastructure.Attribute.Converters;

public class AttributeTypeIdConverterTests
{
    private static object CreateConverter()
    {
        var type = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Attribute.Converters.AttributeTypeIdConverter");

        type.ShouldNotBeNull();
        return Activator.CreateInstance(type!)!;
    }

    private static LambdaExpression TypedExpression(object converter, string propertyName) =>
        (LambdaExpression)converter.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name == propertyName)
            .Single(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition() == typeof(Expression<>))
            .GetValue(converter)!;

    private static LambdaExpression ToProvider(object converter) =>
        TypedExpression(converter, "ConvertToProviderExpression");

    private static LambdaExpression FromProvider(object converter) =>
        TypedExpression(converter, "ConvertFromProviderExpression");

    [Fact]
    public void Converter_DerivesFromStronglyTypedIdConverter()
    {
        var converter = CreateConverter();
        var baseType = converter.GetType().BaseType;

        baseType.ShouldNotBeNull();
        baseType!.Name.ShouldBe("StronglyTypedIdConverter`1");
        baseType.GetGenericArguments()[0].ShouldBe(typeof(AttributeTypeId));
    }

    [Fact]
    public void Converter_MapsBetweenAttributeTypeIdAndGuid()
    {
        dynamic converter = CreateConverter();

        ((Type)converter.ProviderClrType).ShouldBe(typeof(Guid));
        ((Type)converter.ModelClrType).ShouldBe(typeof(AttributeTypeId));
    }

    [Fact]
    public void ConvertToProvider_ReturnsUnderlyingGuid()
    {
        var converter = CreateConverter();
        var id = AttributeTypeId.NewId();

        ToProvider(converter).Compile().DynamicInvoke(id).ShouldBe(id.Value);
    }

    [Fact]
    public void ConvertFromProvider_RestoresStronglyTypedId()
    {
        var converter = CreateConverter();
        var value = Guid.NewGuid();

        FromProvider(converter).Compile().DynamicInvoke(value).ShouldBe(AttributeTypeId.From(value));
    }

    [Fact]
    public void Roundtrip_PreservesIdentity()
    {
        var converter = CreateConverter();
        var original = AttributeTypeId.NewId();
        var toProvider = ToProvider(converter).Compile();
        var fromProvider = FromProvider(converter).Compile();

        fromProvider.DynamicInvoke(toProvider.DynamicInvoke(original)).ShouldBe(original);
    }
}
