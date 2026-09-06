using System.Linq.Expressions;
using System.Reflection;
using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Infrastructure.Wallet.Converters;

public class WalletReservationIdConverterTests
{
    private static object CreateConverter()
    {
        var type = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Wallet.Converters.WalletReservationIdConverter");

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
        baseType.GetGenericArguments()[0].ShouldBe(typeof(WalletReservationId));
    }

    [Fact]
    public void Converter_MapsBetweenWalletReservationIdAndGuid()
    {
        dynamic converter = CreateConverter();

        ((Type)converter.ProviderClrType).ShouldBe(typeof(Guid));
        ((Type)converter.ModelClrType).ShouldBe(typeof(WalletReservationId));
    }

    [Fact]
    public void ConvertToProvider_ReturnsUnderlyingGuid()
    {
        var converter = CreateConverter();
        var id = WalletReservationId.NewId();
        var toProvider = ToProvider(converter).Compile();

        toProvider.DynamicInvoke(id).ShouldBe(id.Value);
    }

    [Fact]
    public void ConvertFromProvider_RestoresStronglyTypedId()
    {
        var converter = CreateConverter();
        var value = Guid.NewGuid();
        var fromProvider = FromProvider(converter).Compile();

        fromProvider.DynamicInvoke(value).ShouldBe(WalletReservationId.From(value));
    }

    [Fact]
    public void Roundtrip_PreservesIdentity()
    {
        var converter = CreateConverter();
        var original = WalletReservationId.NewId();
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
        WalletReservationId.NewId().ShouldNotBe(WalletReservationId.NewId());
    }
}
