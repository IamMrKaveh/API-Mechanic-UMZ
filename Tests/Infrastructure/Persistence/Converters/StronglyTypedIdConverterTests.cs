using System.Linq.Expressions;
using System.Reflection;
using Domain.Product.ValueObjects;
using Domain.Security.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedKernel.Exceptions;

namespace Tests.Infrastructure.Persistence.Converters;

public class StronglyTypedIdConverterTests
{
    private static object CreateConverter(string fullName)
    {
        var type = typeof(DBContext).Assembly.GetType(fullName);
        type.ShouldNotBeNull();
        return Activator.CreateInstance(type!)!;
    }

    private static object CreateProductConverter() =>
        CreateConverter("Infrastructure.Product.Converters.ProductIdConverter");

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
    public void BaseType_IsGenericStronglyTypedIdConverter()
    {
        var converter = CreateProductConverter();
        var baseType = converter.GetType().BaseType;

        baseType.ShouldNotBeNull();
        baseType!.IsGenericType.ShouldBeTrue();
        baseType.GetGenericTypeDefinition().FullName.ShouldBe(
            "Infrastructure.Persistence.Converters.StronglyTypedIdConverter`1");
        baseType.GetGenericArguments()[0].ShouldBe(typeof(ProductId));
    }

    [Fact]
    public void ClrTypes_MapModelToGuidProvider()
    {
        var converter = (ValueConverter)CreateProductConverter();

        converter.ModelClrType.ShouldBe(typeof(ProductId));
        converter.ProviderClrType.ShouldBe(typeof(Guid));
    }

    [Fact]
    public void ConvertToProvider_MapsIdToUnderlyingGuid()
    {
        var converter = CreateProductConverter();
        var id = ProductId.NewId();

        ToProvider(converter).Compile().DynamicInvoke(id).ShouldBe(id.Value);
    }

    [Fact]
    public void ConvertFromProvider_RestoresIdFromGuid()
    {
        var converter = CreateProductConverter();
        var value = Guid.NewGuid();

        FromProvider(converter).Compile().DynamicInvoke(value).ShouldBe(ProductId.From(value));
    }

    [Fact]
    public void Roundtrip_PreservesValueEquality()
    {
        var converter = CreateProductConverter();
        var original = ProductId.NewId();
        var toProvider = ToProvider(converter).Compile();
        var fromProvider = FromProvider(converter).Compile();

        fromProvider.DynamicInvoke(toProvider.DynamicInvoke(original)).ShouldBe(original);
    }

    [Fact]
    public void ConvertToProvider_WithNullId_Throws()
    {
        var converter = CreateProductConverter();
        var toProvider = ToProvider(converter).Compile();

        Should.Throw<TargetInvocationException>(() => toProvider.DynamicInvoke(new object?[] { null }));
    }

    [Fact]
    public void ConvertFromProvider_WithEmptyGuid_ThrowsDomainException()
    {
        var converter = CreateProductConverter();
        var fromProvider = FromProvider(converter).Compile();

        var ex = Should.Throw<TargetInvocationException>(() => fromProvider.DynamicInvoke(Guid.Empty));
        ex.InnerException.ShouldBeOfType<DomainException>();
    }

    [Fact]
    public void Expressions_AreDistinctPerDirection()
    {
        var converter = CreateProductConverter();

        ToProvider(converter).ShouldNotBeSameAs(FromProvider(converter));
    }

    [Fact]
    public void DistinctConverterInstances_DoNotShareState()
    {
        var first = CreateProductConverter();
        var second = CreateProductConverter();

        first.ShouldNotBeSameAs(second);
        ToProvider(first).Compile().DynamicInvoke(ProductId.NewId()).ShouldBeOfType<Guid>();
        FromProvider(second).Compile().DynamicInvoke(Guid.NewGuid()).ShouldBeOfType<ProductId>();
    }

    [Fact]
    public void BaseContract_HoldsAcrossDifferentIdTypes()
    {
        var product = (ValueConverter)CreateProductConverter();
        var otp = (ValueConverter)CreateConverter("Infrastructure.Security.Converters.OtpIdConverter");

        product.ProviderClrType.ShouldBe(typeof(Guid));
        otp.ProviderClrType.ShouldBe(typeof(Guid));
        product.ModelClrType.ShouldBe(typeof(ProductId));
        otp.ModelClrType.ShouldBe(typeof(OtpId));

        var productId = ProductId.NewId();
        var otpId = OtpId.NewId();

        ToProvider(product).Compile().DynamicInvoke(productId).ShouldBe(productId.Value);
        ToProvider(otp).Compile().DynamicInvoke(otpId).ShouldBe(otpId.Value);
        FromProvider(otp).Compile().DynamicInvoke(otpId.Value).ShouldBe(otpId);
    }

    [Fact]
    public void ConvertFromProvider_WithSameGuid_ReturnsEqualIds()
    {
        var converter = CreateProductConverter();
        var value = Guid.NewGuid();
        var fromProvider = FromProvider(converter).Compile();

        fromProvider.DynamicInvoke(value).ShouldBe(fromProvider.DynamicInvoke(value));
    }
}
