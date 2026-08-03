using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Discount.ValueObjects;

public class DiscountValueTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void Percentage_WithValueInRange_ReturnsPercentageDiscountValue(decimal percent)
    {
        var sut = DiscountValue.Percentage(percent);

        sut.Type.ShouldBe(DiscountType.Percentage);
        sut.Amount.ShouldBe(percent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(200)]
    public void Percentage_WithValueOutOfRange_ThrowsDomainException(decimal percent)
    {
        Should.Throw<DomainException>(() => DiscountValue.Percentage(percent));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void Fixed_WithPositiveAmount_ReturnsFixedDiscountValue(decimal amount)
    {
        var sut = DiscountValue.Fixed(amount);

        sut.Type.ShouldBe(DiscountType.FixedAmount);
        sut.Amount.ShouldBe(amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Fixed_WithNonPositiveAmount_ThrowsDomainException(decimal amount)
    {
        Should.Throw<DomainException>(() => DiscountValue.Fixed(amount));
    }

    [Fact]
    public void FreeShipping_ReturnsValueWithZeroAmountAndFreeShippingType()
    {
        var sut = DiscountValue.FreeShipping();

        sut.Type.ShouldBe(DiscountType.FreeShipping);
        sut.Amount.ShouldBe(0m);
    }

    [Theory]
    [InlineData(20, 100, 80)]
    [InlineData(10, 200, 180)]
    [InlineData(50, 100, 50)]
    [InlineData(100, 100, 0)]
    public void Apply_Percentage_ReturnsOriginalMinusPercentage(
        decimal percent, decimal orderAmount, decimal expected)
    {
        var sut = DiscountValue.Percentage(percent);
        var order = Money.Create(orderAmount, "IRT");

        sut.Apply(order).Amount.ShouldBe(expected);
    }

    [Fact]
    public void Apply_FixedWhenOrderExceedsDiscount_ReturnsOrderMinusFixed()
    {
        var sut = DiscountValue.Fixed(30m);
        var order = Money.Create(100m, "IRT");

        sut.Apply(order).Amount.ShouldBe(70m);
    }

    [Fact]
    public void Apply_FixedWhenDiscountExceedsOrder_ReturnsZero()
    {
        var sut = DiscountValue.Fixed(500m);
        var order = Money.Create(100m, "IRT");

        sut.Apply(order).Amount.ShouldBe(0m);
        sut.Apply(order).Currency.ShouldBe("IRT");
    }

    [Fact]
    public void Apply_FixedWhenDiscountEqualsOrder_ReturnsZero()
    {
        var sut = DiscountValue.Fixed(100m);
        var order = Money.Create(100m, "IRT");

        sut.Apply(order).Amount.ShouldBe(0m);
    }

    [Fact]
    public void Apply_FreeShipping_ReturnsOriginalPriceUnchanged()
    {
        var sut = DiscountValue.FreeShipping();
        var order = Money.Create(100m, "IRT");

        sut.Apply(order).Amount.ShouldBe(100m);
    }

    [Fact]
    public void Equality_ForSameAmountAndType_TreatsInstancesAsEqual()
    {
        DiscountValue.Percentage(20m).ShouldBe(DiscountValue.Percentage(20m));
    }

    [Fact]
    public void Equality_ForSameAmountDifferentType_TreatsInstancesAsUnequal()
    {
        DiscountValue.Percentage(20m).ShouldNotBe(DiscountValue.Fixed(20m));
    }

    [Fact]
    public void Equality_ForDifferentAmountSameType_TreatsInstancesAsUnequal()
    {
        DiscountValue.Percentage(20m).ShouldNotBe(DiscountValue.Percentage(30m));
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        DiscountValue.Percentage(10m).ShouldBeAssignableTo<ValueObject>();
    }
}
