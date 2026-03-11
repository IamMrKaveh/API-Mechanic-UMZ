using Domain.Common.ValueObjects;

namespace Tests.DomainTest.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void FromDecimal_ShouldCreateMoneyWithCorrectAmount()
    {
        var money = Money.FromDecimal(100_000m);

        money.Amount.Should().Be(100_000m);
        money.Currency.Should().Be("IRR");
    }

    [Fact]
    public void Zero_ShouldHaveZeroAmount()
    {
        var money = Money.Zero();

        money.IsZero().Should().BeTrue();
        money.Amount.Should().Be(0);
    }

    [Fact]
    public void Add_ShouldSumAmounts()
    {
        var a = Money.FromDecimal(100_000m);
        var b = Money.FromDecimal(50_000m);

        var result = a.Add(b);

        result.Amount.Should().Be(150_000m);
    }

    [Fact]
    public void Add_WithOperator_ShouldSumAmounts()
    {
        var a = Money.FromDecimal(200_000m);
        var b = Money.FromDecimal(100_000m);

        var result = a + b;

        result.Amount.Should().Be(300_000m);
    }

    [Fact]
    public void Subtract_WhenResultIsPositive_ShouldReturnDifference()
    {
        var a = Money.FromDecimal(300_000m);
        var b = Money.FromDecimal(100_000m);

        var result = a.Subtract(b);

        result.Amount.Should().Be(200_000m);
    }

    [Fact]
    public void Subtract_WhenResultIsNegative_ShouldThrowDomainException()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(1_000m);

        var act = () => a.Subtract(b);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Multiply_WithPositiveFactor_ShouldMultiply()
    {
        var money = Money.FromDecimal(100_000m);

        var result = money.Multiply(3m);

        result.Amount.Should().Be(300_000m);
    }

    [Fact]
    public void Multiply_WithNegativeFactor_ShouldThrowDomainException()
    {
        var money = Money.FromDecimal(100_000m);

        var act = () => money.Multiply(-1m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Divide_WithNonZeroDivisor_ShouldDivide()
    {
        var money = Money.FromDecimal(300_000m);

        var result = money.Divide(3m);

        result.Amount.Should().Be(100_000m);
    }

    [Fact]
    public void Divide_ByZero_ShouldThrowDomainException()
    {
        var money = Money.FromDecimal(100_000m);

        var act = () => money.Divide(0m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Percentage_WithValidPercentage_ShouldCalculateCorrectly()
    {
        var money = Money.FromDecimal(200_000m);

        var result = money.Percentage(10m);

        result.Amount.Should().Be(20_000m);
    }

    [Fact]
    public void Percentage_WithNegativeValue_ShouldThrowDomainException()
    {
        var money = Money.FromDecimal(100_000m);

        var act = () => money.Percentage(-10m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsPositive_WithPositiveAmount_ShouldReturnTrue()
    {
        var money = Money.FromDecimal(1m);

        money.IsPositive().Should().BeTrue();
    }

    [Fact]
    public void IsNegative_WithNegativeAmount_ShouldReturnTrue()
    {
        var money = Money.FromDecimal(-1m);

        money.IsNegative().Should().BeTrue();
    }

    [Fact]
    public void IsGreaterThan_WhenGreater_ShouldReturnTrue()
    {
        var a = Money.FromDecimal(1_000m);
        var b = Money.FromDecimal(500m);

        a.IsGreaterThan(b).Should().BeTrue();
    }

    [Fact]
    public void IsLessThan_WhenLess_ShouldReturnTrue()
    {
        var a = Money.FromDecimal(200m);
        var b = Money.FromDecimal(1_000m);

        a.IsLessThan(b).Should().BeTrue();
    }

    [Fact]
    public void TwoMoneyObjects_WithSameAmount_ShouldBeEqual()
    {
        var a = Money.FromDecimal(500_000m);
        var b = Money.FromDecimal(500_000m);

        a.Should().Be(b);
    }

    [Fact]
    public void TwoMoneyObjects_WithDifferentAmounts_ShouldNotBeEqual()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(200m);

        a.Should().NotBe(b);
    }

    [Fact]
    public void FromToman_ShouldMultiplyByTen()
    {
        var money = Money.FromToman(10_000m);

        money.Amount.Should().Be(100_000m);
    }

    [Fact]
    public void ToToman_ShouldDivideByTen()
    {
        var money = Money.FromDecimal(100_000m);

        money.ToToman().Should().Be(10_000m);
    }

    [Fact]
    public void Abs_WhenNegative_ShouldReturnPositive()
    {
        var money = Money.FromDecimal(-300_000m);

        var result = money.Abs();

        result.Amount.Should().Be(300_000m);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ShouldThrowDomainException()
    {
        var irr = Money.FromDecimal(100m, "IRR");
        var usd = Money.FromDecimal(1m, "USD");

        var act = () => irr.Add(usd);

        act.Should().Throw<DomainException>();
    }
}