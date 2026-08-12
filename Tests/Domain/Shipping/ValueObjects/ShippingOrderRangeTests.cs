using Domain.Shipping.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Domain.Shipping.ValueObjects;

public class ShippingOrderRangeTests
{
    [Fact]
    public void Unlimited_ProducesRangeWithoutMinimumOrMaximum()
    {
        var sut = ShippingOrderRange.Unlimited();

        sut.HasMinimum.ShouldBeFalse();
        sut.HasMaximum.ShouldBeFalse();
        sut.MinOrderAmount.ShouldBeNull();
        sut.MaxOrderAmount.ShouldBeNull();
    }

    [Fact]
    public void IsInRange_ForUnlimitedRange_ReturnsTrueForAnyPositiveAmount()
    {
        ShippingOrderRange.Unlimited().IsInRange(Money.FromDecimal(1m)).ShouldBeTrue();
        ShippingOrderRange.Unlimited().IsInRange(Money.FromDecimal(999_999_999m)).ShouldBeTrue();
    }

    [Fact]
    public void IsInRange_ForUnlimitedRange_ReturnsTrueForZeroAmount()
    {
        ShippingOrderRange.Unlimited().IsInRange(Money.Zero()).ShouldBeTrue();
    }

    [Fact]
    public void Validate_ForUnlimitedRange_ReturnsSuccessForAnyAmount()
    {
        ShippingOrderRange.Unlimited().Validate(Money.FromDecimal(10_000m)).ShouldBeSuccess();
    }

    [Fact]
    public void Equality_ForBothUnlimited_TreatsInstancesAsEqual()
    {
        ShippingOrderRange.Unlimited().ShouldBe(ShippingOrderRange.Unlimited());
    }
}
