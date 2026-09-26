using System.Linq.Expressions;
using System.Reflection;
using Domain.Product.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Infrastructure.Persistence.Converters;

public class StronglyTypedIdConverterTests
{
    public static TheoryData<Type> AllStronglyTypedIds
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in typeof(ProductId).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract
                    && t.BaseType is { IsGenericType: true }
                    && t.BaseType.GetGenericTypeDefinition() == typeof(StronglyTypedId<>))
                .OrderBy(t => t.Name))
            {
                data.Add(type);
            }

            return data;
        }
    }

    private static object CreateConverter(Type idType)
    {
        var converterType = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Persistence.Converters.StronglyTypedIdConverter`1")!
            .MakeGenericType(idType);

        return Activator.CreateInstance(converterType)!;
    }

    private static MethodInfo StronglyTypedIdMethod(Type idType, string name) =>
        idType.GetMethod(
            name,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!;

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
    public void ConverterType_IsGenericStronglyTypedIdConverter()
    {
        var converterType = typeof(DBContext).Assembly.GetType(
            "Infrastructure.Persistence.Converters.StronglyTypedIdConverter`1");

        converterType.ShouldNotBeNull();
        converterType!.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(AllStronglyTypedIds))]
    public void ClrTypes_MapModelToGuidProvider(Type idType)
    {
        var converter = (ValueConverter)CreateConverter(idType);

        converter.ModelClrType.ShouldBe(idType);
        converter.ProviderClrType.ShouldBe(typeof(Guid));
    }

    [Theory]
    [MemberData(nameof(AllStronglyTypedIds))]
    public void ConvertToProvider_MapsIdToUnderlyingGuid(Type idType)
    {
        var converter = CreateConverter(idType);
        var from = StronglyTypedIdMethod(idType, "From");
        var value = Guid.NewGuid();
        var id = from.Invoke(null, [value])!;

        ToProvider(converter).Compile().DynamicInvoke(id).ShouldBe(value);
    }

    [Theory]
    [MemberData(nameof(AllStronglyTypedIds))]
    public void ConvertFromProvider_RestoresIdFromGuid(Type idType)
    {
        var converter = CreateConverter(idType);
        var from = StronglyTypedIdMethod(idType, "From");
        var value = Guid.NewGuid();

        FromProvider(converter).Compile().DynamicInvoke(value).ShouldBe(from.Invoke(null, [value]));
    }

    [Theory]
    [MemberData(nameof(AllStronglyTypedIds))]
    public void Roundtrip_PreservesValueEquality(Type idType)
    {
        var converter = CreateConverter(idType);
        var newId = StronglyTypedIdMethod(idType, "NewId");
        var original = newId.Invoke(null, null)!;
        var toProvider = ToProvider(converter).Compile();
        var fromProvider = FromProvider(converter).Compile();

        fromProvider.DynamicInvoke(toProvider.DynamicInvoke(original)).ShouldBe(original);
    }

    [Theory]
    [MemberData(nameof(AllStronglyTypedIds))]
    public void ConvertFromProvider_WithEmptyGuid_ThrowsDomainException(Type idType)
    {
        var converter = CreateConverter(idType);
        var fromProvider = FromProvider(converter).Compile();

        var ex = Should.Throw<TargetInvocationException>(() => fromProvider.DynamicInvoke(Guid.Empty));
        ex.InnerException.ShouldBeOfType<DomainException>();
    }

    [Theory]
    [MemberData(nameof(AllStronglyTypedIds))]
    public void Expressions_AreDistinctPerDirection(Type idType)
    {
        var converter = CreateConverter(idType);

        ToProvider(converter).ShouldNotBeSameAs(FromProvider(converter));
    }

    [Theory]
    [MemberData(nameof(AllStronglyTypedIds))]
    public void DistinctConverterInstances_DoNotShareState(Type idType)
    {
        var first = CreateConverter(idType);
        var second = CreateConverter(idType);

        first.ShouldNotBeSameAs(second);
    }
}
