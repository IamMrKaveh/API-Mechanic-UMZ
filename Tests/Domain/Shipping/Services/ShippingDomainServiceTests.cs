using Domain.Shipping.Services;
using Domain.Shipping.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Shipping.Services;

public class ShippingDomainServiceTests
{
    private static global::Domain.Shipping.Aggregates.Shipping NewShipping() =>
        new ShippingBuilder().WithBaseCost(80_000m, "IRT").Build();

    [Fact]
    public void CalculateShippingCost_WithNullShipping_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ShippingDomainService.CalculateShippingCost(null!, Money.FromDecimal(500_000m)));
    }

    [Fact]
    public void CalculateShippingCost_WithNullOrderTotal_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ShippingDomainService.CalculateShippingCost(NewShipping(), null!));
    }

    [Fact]
    public void CalculateShippingCost_WhenShippingIsInactive_ReturnsNotAvailable()
    {
        var shipping = NewShipping();
        shipping.RequestDeletion();

        var result = ShippingDomainService.CalculateShippingCost(
            shipping, Money.FromDecimal(500_000m));

        result.IsSuccess.ShouldBeFalse();
        result.ShippingId.ShouldBe(shipping.Id);
        result.Error.ShouldNotBeNullOrWhiteSpace();
        result.Cost.ShouldBeNull();
    }

    [Fact]
    public void CalculateShippingCost_WithoutItems_ReturnsBaseCostCalculation()
    {
        var shipping = NewShipping();
        var orderTotal = Money.FromDecimal(500_000m);

        var result = ShippingDomainService.CalculateShippingCost(shipping, orderTotal);

        result.IsSuccess.ShouldBeTrue();
        result.Cost.ShouldBe(shipping.CalculateCost(orderTotal));
        result.IsFreeShipping.ShouldBeFalse();
        result.DeliveryTimeDisplay.ShouldBe(shipping.GetDeliveryTimeDisplay());
    }

    [Fact]
    public void CalculateShippingCost_WithEmptyItems_FallsBackToBaseCostCalculation()
    {
        var shipping = NewShipping();
        var orderTotal = Money.FromDecimal(500_000m);

        var result = ShippingDomainService.CalculateShippingCost(
            shipping, orderTotal, []);

        result.IsSuccess.ShouldBeTrue();
        result.Cost.ShouldBe(shipping.CalculateCost(orderTotal));
    }

    [Fact]
    public void CalculateShippingCost_WithItems_UsesPerCartCalculation()
    {
        var shipping = NewShipping();
        var orderTotal = Money.FromDecimal(500_000m);
        var items = new[]
        {
            new ShippingCostItem(VariantId.NewId(), 2m, 1),
            new ShippingCostItem(VariantId.NewId(), 1m, 3)
        };

        var result = ShippingDomainService.CalculateShippingCost(shipping, orderTotal, items);

        result.IsSuccess.ShouldBeTrue();
        result.Cost.ShouldBe(shipping.CalculateCostForCart(orderTotal, items));
    }
}
