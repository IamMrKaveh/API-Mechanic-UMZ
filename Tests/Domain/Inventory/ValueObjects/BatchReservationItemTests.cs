using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.Domain.Inventory.ValueObjects;

public class BatchReservationItemTests
{
    [Fact]
    public void Ctor_AssignsVariantIdAndQuantity()
    {
        var variantId = VariantId.NewId();
        var quantity = StockQuantity.Create(3);

        var sut = new BatchReservationItem(variantId, quantity);

        sut.VariantId.ShouldBe(variantId);
        sut.Quantity.ShouldBe(quantity);
    }

    [Fact]
    public void Equality_TwoItemsWithSameVariantIdAndQuantity_TreatedAsEqual()
    {
        var variantId = VariantId.NewId();
        var quantity = StockQuantity.Create(3);

        var a = new BatchReservationItem(variantId, quantity);
        var b = new BatchReservationItem(variantId, quantity);

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_TwoItemsWithDifferentQuantity_TreatedAsUnequal()
    {
        var variantId = VariantId.NewId();

        var a = new BatchReservationItem(variantId, StockQuantity.Create(3));
        var b = new BatchReservationItem(variantId, StockQuantity.Create(4));

        a.ShouldNotBe(b);
    }
}
