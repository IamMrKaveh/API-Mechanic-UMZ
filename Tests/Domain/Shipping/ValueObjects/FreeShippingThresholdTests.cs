using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Shipping.ValueObjects;

public class FreeShippingThresholdTests
{
    [Fact]
    public void Disabled_ReturnsInstanceWithIsEnabledFalseAndNoThreshold()
    {
        var sut = FreeShippingThreshold.Disabled();

        sut.IsEnabled.ShouldBeFalse();
        sut.ThresholdAmount.ShouldBeNull();
    }

    [Fact]
    public void Enabled_WithPositiveThreshold_StoresThresholdAndMarksEnabled()
    {
        var threshold = Money.FromDecimal(500_000m);

        var sut = FreeShippingThreshold.Enabled(threshold);

        sut.IsEnabled.ShouldBeTrue();
        sut.ThresholdAmount.ShouldBe(threshold);
    }

    [Fact]
    public void Enabled_WithZeroThreshold_IsAllowed()
    {
        Should.NotThrow(() => FreeShippingThreshold.Enabled(Money.Zero()));
    }

    [Fact]
    public void Enabled_WithNullThreshold_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => FreeShippingThreshold.Enabled(null!));
    }

    [Fact]
    public void Restore_WithEnabledTrueAndNullThreshold_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => FreeShippingThreshold.Restore(true, null));
    }

    [Fact]
    public void Restore_WithDisabledStateAndNullThreshold_Succeeds()
    {
        var sut = FreeShippingThreshold.Restore(false, null);

        sut.IsEnabled.ShouldBeFalse();
        sut.ThresholdAmount.ShouldBeNull();
    }

    [Fact]
    public void Restore_WithEnabledStateAndThreshold_PreservesBothValues()
    {
        var threshold = Money.FromDecimal(1_000_000m);

        var sut = FreeShippingThreshold.Restore(true, threshold);

        sut.IsEnabled.ShouldBeTrue();
        sut.ThresholdAmount.ShouldBe(threshold);
    }

    [Fact]
    public void QualifiesForFreeShipping_WhenDisabled_ReturnsFalseRegardlessOfOrderTotal()
    {
        FreeShippingThreshold.Disabled()
            .QualifiesForFreeShipping(Money.FromDecimal(10_000_000m))
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(499_999)]
    [InlineData(0)]
    public void QualifiesForFreeShipping_WhenOrderBelowThreshold_ReturnsFalse(decimal orderAmount)
    {
        var sut = FreeShippingThreshold.Enabled(Money.FromDecimal(500_000m));

        sut.QualifiesForFreeShipping(Money.FromDecimal(orderAmount)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(500_000)]
    [InlineData(500_001)]
    [InlineData(1_000_000)]
    public void QualifiesForFreeShipping_WhenOrderMeetsOrExceedsThreshold_ReturnsTrue(decimal orderAmount)
    {
        var sut = FreeShippingThreshold.Enabled(Money.FromDecimal(500_000m));

        sut.QualifiesForFreeShipping(Money.FromDecimal(orderAmount)).ShouldBeTrue();
    }

    [Fact]
    public void Equality_ForSameEnabledStateAndAmount_TreatsInstancesAsEqual()
    {
        var a = FreeShippingThreshold.Enabled(Money.FromDecimal(500_000m));
        var b = FreeShippingThreshold.Enabled(Money.FromDecimal(500_000m));

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_ForDifferentAmount_TreatsInstancesAsNotEqual()
    {
        var a = FreeShippingThreshold.Enabled(Money.FromDecimal(500_000m));
        var b = FreeShippingThreshold.Enabled(Money.FromDecimal(600_000m));

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Equality_ForDisabledVersusEnabled_TreatsInstancesAsNotEqual()
    {
        FreeShippingThreshold.Disabled()
            .ShouldNotBe(FreeShippingThreshold.Enabled(Money.FromDecimal(500_000m)));
    }
}
