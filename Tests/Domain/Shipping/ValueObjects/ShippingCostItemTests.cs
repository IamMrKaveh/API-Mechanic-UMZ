using Domain.Shipping.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.Domain.Shipping.ValueObjects;

public class ShippingCostItemTests
{
    [Fact]
    public void Constructor_WithValidValues_StoresAllFields()
    {
        var variantId = VariantId.NewId();

        var sut = new ShippingCostItem(variantId, 1.25m, 3);

        sut.VariantId.ShouldBe(variantId);
        sut.ShippingMultiplier.ShouldBe(1.25m);
        sut.Quantity.ShouldBe(3);
    }

    [Fact]
    public void Equality_ForSameFieldValues_TreatsInstancesAsEqual()
    {
        var variantId = VariantId.NewId();

        var a = new ShippingCostItem(variantId, 1m, 2);
        var b = new ShippingCostItem(variantId, 1m, 2);

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_ForDifferentMultiplier_TreatsInstancesAsNotEqual()
    {
        var variantId = VariantId.NewId();

        new ShippingCostItem(variantId, 1m, 2)
            .ShouldNotBe(new ShippingCostItem(variantId, 2m, 2));
    }

    [Fact]
    public void Equality_ForDifferentQuantity_TreatsInstancesAsNotEqual()
    {
        var variantId = VariantId.NewId();

        new ShippingCostItem(variantId, 1m, 2)
            .ShouldNotBe(new ShippingCostItem(variantId, 1m, 3));
    }

    [Fact]
    public void Equality_ForDifferentVariantId_TreatsInstancesAsNotEqual()
    {
        new ShippingCostItem(VariantId.NewId(), 1m, 2)
            .ShouldNotBe(new ShippingCostItem(VariantId.NewId(), 1m, 2));
    }

    [Fact]
    public void WithExpression_ProducesModifiedCopyLeavingOriginalIntact()
    {
        var variantId = VariantId.NewId();
        var original = new ShippingCostItem(variantId, 1m, 2);

        var modified = original with { Quantity = 10 };

        original.Quantity.ShouldBe(2);
        modified.Quantity.ShouldBe(10);
    }
}
