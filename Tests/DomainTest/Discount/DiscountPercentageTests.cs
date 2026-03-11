using Domain.Common.ValueObjects;

namespace Tests.DomainTest.Discount;

public class DiscountPercentageTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(99.99)]
    [InlineData(100)]
    public void Create_WithValidPercentage_ShouldSucceed(decimal value)
    {
        var result = DiscountPercentage.Create(value);

        result.Should().NotBeNull();
        result.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_WithZeroOrNegativePercentage_ShouldThrowDomainException(decimal value)
    {
        var act = () => DiscountPercentage.Create(value);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(100.01)]
    [InlineData(150)]
    [InlineData(200)]
    public void Create_WithPercentageGreaterThan100_ShouldThrowDomainException(decimal value)
    {
        var act = () => DiscountPercentage.Create(value);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnCorrectAmount()
    {
        var percentage = DiscountPercentage.Create(20);

        var discount = percentage.CalculateDiscount(1000);

        discount.Should().Be(200);
    }

    [Fact]
    public void CalculateDiscount_ShouldRoundToZeroDecimalPlaces()
    {
        var percentage = DiscountPercentage.Create(10);

        var discount = percentage.CalculateDiscount(155);

        discount.Should().Be(16);
    }

    [Fact]
    public void CalculateDiscountMoney_ShouldReturnMoneyObject()
    {
        var percentage = DiscountPercentage.Create(10);
        var amount = Money.FromDecimal(1000);

        var result = percentage.CalculateDiscountMoney(amount);

        result.Amount.Should().Be(100);
    }

    [Fact]
    public void TwoPercentages_WithSameValue_ShouldBeEqual()
    {
        var p1 = DiscountPercentage.Create(10);
        var p2 = DiscountPercentage.Create(10);

        p1.Should().Be(p2);
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ShouldWork()
    {
        var percentage = DiscountPercentage.Create(25);

        decimal result = percentage;

        result.Should().Be(25);
    }

    [Fact]
    public void ToString_ShouldIncludePercentSign()
    {
        var percentage = DiscountPercentage.Create(10);

        percentage.ToString().Should().Contain("%");
    }

    [Fact]
    public void IsZero_ShouldBeFalse_ForNonZeroPercentage()
    {
        var percentage = DiscountPercentage.Create(10);

        percentage.IsZero.Should().BeFalse();
    }

    [Fact]
    public void IsFull_ShouldBeTrue_For100Percentage()
    {
        var percentage = DiscountPercentage.Create(100);

        percentage.IsFull.Should().BeTrue();
    }
}