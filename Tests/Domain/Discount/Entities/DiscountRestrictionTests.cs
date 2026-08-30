using System.Reflection;
using Domain.Discount.Aggregates;
using Domain.Discount.Entities;
using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;
using SharedKernel.Abstractions;

namespace Tests.Domain.Discount.Entities;

public class DiscountRestrictionTests
{
    [Fact]
    public void Type_IsSealed()
    {
        typeof(DiscountRestriction).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void Type_InheritsFromEntityOfDiscountRestrictionId()
    {
        typeof(DiscountRestriction).BaseType!.ShouldBe(typeof(Entity<DiscountRestrictionId>));
    }

    [Fact]
    public void Type_HasPrivateParameterlessConstructor()
    {
        var ctor = typeof(DiscountRestriction).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        ctor.ShouldNotBeNull();
        ctor.IsPrivate.ShouldBeTrue();
    }

    [Fact]
    public void Type_ExposesNoPublicMutators()
    {
        var publicMutators = typeof(DiscountRestriction)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.SetMethod is not null && p.SetMethod.IsPublic)
            .ToList();

        publicMutators.ShouldBeEmpty();
    }

    [Fact]
    public void Type_ExposesDiscountCodeIdAndDiscountCodeNavigation()
    {
        var discountCodeIdProp = typeof(DiscountRestriction).GetProperty("DiscountCodeId");
        var discountCodeProp = typeof(DiscountRestriction).GetProperty("DiscountCode");

        discountCodeIdProp.ShouldNotBeNull();
        discountCodeIdProp.PropertyType.ShouldBe(typeof(DiscountCodeId));
        discountCodeProp.ShouldNotBeNull();
        discountCodeProp.PropertyType.ShouldBe(typeof(DiscountCode));
    }

    [Fact]
    public void Type_ExposesRestrictionTypeAndRestrictionValue()
    {
        var typeProp = typeof(DiscountRestriction).GetProperty("RestrictionType");
        var valueProp = typeof(DiscountRestriction).GetProperty("RestrictionValue");

        typeProp.ShouldNotBeNull();
        typeProp.PropertyType.ShouldBe(typeof(DiscountRestrictionType));
        valueProp.ShouldNotBeNull();
        valueProp.PropertyType.ShouldBe(typeof(string));
    }

    [Fact]
    public void Equality_WhenIdsAreEqual_TreatsInstancesAsEqual()
    {
        var id = DiscountRestrictionId.NewId();
        var first = CreateWithId(id);
        var second = CreateWithId(id);

        first.Equals(second).ShouldBeTrue();
        (first == second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equality_WhenIdsDiffer_TreatsInstancesAsNotEqual()
    {
        var first = CreateWithId(DiscountRestrictionId.NewId());
        var second = CreateWithId(DiscountRestrictionId.NewId());

        first.Equals(second).ShouldBeFalse();
        (first != second).ShouldBeTrue();
    }

    [Fact]
    public void Equality_AgainstNullOrDifferentType_ReturnsFalse()
    {
        var instance = CreateWithId(DiscountRestrictionId.NewId());

        instance.Equals(null).ShouldBeFalse();
        instance.Equals("not a restriction").ShouldBeFalse();
    }

    [Fact]
    public void RestrictionTypeEnum_ContainsAllExpectedMembers()
    {
        Enum.IsDefined(typeof(DiscountRestrictionType), DiscountRestrictionType.MinimumOrderAmount).ShouldBeTrue();
        Enum.IsDefined(typeof(DiscountRestrictionType), DiscountRestrictionType.SpecificProduct).ShouldBeTrue();
        Enum.IsDefined(typeof(DiscountRestrictionType), DiscountRestrictionType.SpecificCategory).ShouldBeTrue();
        Enum.IsDefined(typeof(DiscountRestrictionType), DiscountRestrictionType.SpecificUser).ShouldBeTrue();
        Enum.IsDefined(typeof(DiscountRestrictionType), DiscountRestrictionType.FirstOrderOnly).ShouldBeTrue();
        Enum.IsDefined(typeof(DiscountRestrictionType), DiscountRestrictionType.MaximumUsagePerUser).ShouldBeTrue();
    }

    private static DiscountRestriction CreateWithId(DiscountRestrictionId id)
    {
        var ctor = typeof(DiscountRestriction).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null)!;
        var instance = (DiscountRestriction)ctor.Invoke(null);

        var idProperty = typeof(Entity<DiscountRestrictionId>)
            .GetProperty(nameof(Entity<DiscountRestrictionId>.Id))!;
        idProperty.SetValue(instance, id);

        return instance;
    }
}
