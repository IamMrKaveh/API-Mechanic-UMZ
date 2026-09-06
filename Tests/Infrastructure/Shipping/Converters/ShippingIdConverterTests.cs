using System.Linq.Expressions;
using System.Reflection;
using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Infrastructure.Shipping.Converters;

public class ShippingIdConverterTests
{
    private static object CreateConverter()
    {
        var type = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Shipping.Converters.ShippingIdConverter");

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
        baseType.GetGenericArguments()[0].ShouldBe(typeof(ShippingId));
    }

    [Fact]
    public void Converter_MapsBetweenShippingIdAndGuid()
    {
        dynamic converter = CreateConverter();

        ((Type)converter.ProviderClrType).ShouldBe(typeof(Guid));
        ((Type)converter.ModelClrType).ShouldBe(typeof(ShippingId));
    }

    [Fact]
    public void ConvertToProvider_ReturnsUnderlyingGuid()
    {
        var converter = CreateConverter();
        var id = ShippingId.NewId();
        var toProvider = ToProvider(converter).Compile();

        toProvider.DynamicInvoke(id).ShouldBe(id.Value);
    }

    [Fact]
    public void ConvertFromProvider_RestoresStronglyTypedId()
    {
        var converter = CreateConverter();
        var value = Guid.NewGuid();
        var fromProvider = FromProvider(converter).Compile();

        fromProvider.DynamicInvoke(value).ShouldBe(ShippingId.From(value));
    }

    [Fact]
    public void Roundtrip_PreservesIdentity()
    {
        var converter = CreateConverter();
        var original = ShippingId.NewId();
        var toProvider = ToProvider(converter).Compile();
        var fromProvider = FromProvider(converter).Compile();

        fromProvider.DynamicInvoke(toProvider.DynamicInvoke(original)).ShouldBe(original);
    }

    [Fact]
    public void ConvertFromProvider_WithEmptyGuid_ThrowsDomainException()
    {
        var converter = CreateConverter();
        var fromProvider = FromProvider(converter).Compile();

        var ex = Should.Throw<TargetInvocationException>(() => fromProvider.DynamicInvoke(Guid.Empty));
        ex.InnerException.ShouldBeOfType<DomainException>();
    }

    [Fact]
    public void NewIds_AreUnique()
    {
        ShippingId.NewId().ShouldNotBe(ShippingId.NewId());
    }
}
