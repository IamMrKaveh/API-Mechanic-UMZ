using Domain.Shipping.Results;
using Domain.Shipping.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Shipping.Results;

public class ShippingCostCalculationResultTests
{
    [Fact]
    public void Success_ProducesSuccessfulResultWithProvidedValues()
    {
        var shippingId = ShippingId.NewId();
        var cost = Money.FromDecimal(50_000m);

        var sut = ShippingCostCalculationResult.Success(shippingId, cost, isFreeShipping: false, "۲ تا ۴ روز");

        sut.IsSuccess.ShouldBeTrue();
        sut.ShippingId.ShouldBe(shippingId);
        sut.Cost.ShouldBe(cost);
        sut.IsFreeShipping.ShouldBeFalse();
        sut.DeliveryTimeDisplay.ShouldBe("۲ تا ۴ روز");
        sut.Error.ShouldBeNull();
    }

    [Fact]
    public void Success_WithIsFreeShippingTrue_PreservesFlag()
    {
        var sut = ShippingCostCalculationResult.Success(
            ShippingId.NewId(),
            Money.Zero(),
            isFreeShipping: true,
            "۲ تا ۴ روز");

        sut.IsFreeShipping.ShouldBeTrue();
    }

    [Fact]
    public void NotAvailable_ProducesFailureResultWithErrorMessage()
    {
        var shippingId = ShippingId.NewId();

        var sut = ShippingCostCalculationResult.NotAvailable(shippingId, "روش ارسال غیرفعال است.");

        sut.IsSuccess.ShouldBeFalse();
        sut.ShippingId.ShouldBe(shippingId);
        sut.Cost.ShouldBeNull();
        sut.IsFreeShipping.ShouldBeFalse();
        sut.DeliveryTimeDisplay.ShouldBeNull();
        sut.Error.ShouldBe("روش ارسال غیرفعال است.");
    }
}
