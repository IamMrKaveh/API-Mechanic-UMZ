using Domain.Inventory.Events;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.Domain.Inventory.Events;

public class StockAdjustedEventTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(-50, false)]
    public void IsIncrease_ReflectsSignOfAdjustment(int adjustment, bool expected)
    {
        var sut = new StockAdjustedEvent(
            InventoryId.NewId(),
            VariantId.NewId(),
            newQuantity: 10,
            adjustment: adjustment,
            reason: "x");

        sut.IsIncrease.ShouldBe(expected);
    }

    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(10, -3, 13)]
    [InlineData(5, 0, 5)]
    public void PreviousQuantity_EqualsNewQuantityMinusAdjustment(
        int newQuantity, int adjustment, int expectedPrevious)
    {
        var sut = new StockAdjustedEvent(
            InventoryId.NewId(),
            VariantId.NewId(),
            newQuantity,
            adjustment,
            "x");

        sut.PreviousQuantity.ShouldBe(expectedPrevious);
    }
}
