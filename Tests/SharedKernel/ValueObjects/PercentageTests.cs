using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class PercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(50)]
    [InlineData(99.99)]
    [InlineData(100)]
    public void Create_WithValueInRange_ReturnsPercentage(decimal value)
    {
        var sut = Percentage.Create(value);

        sut.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(200)]
    public void Create_WithValueOutOfRange_ThrowsArgumentException(decimal value)
    {
        Should.Throw<ArgumentException>(() => Percentage.Create(value));
    }

    [Fact]
    public void Zero_ReturnsPercentageWithValueZero()
    {
        Percentage.Zero.Value.ShouldBe(0m);
    }

    [Fact]
    public void Hundred_ReturnsPercentageWithValueHundred()
    {
        Percentage.Hundred.Value.ShouldBe(100m);
    }

    [Theory]
    [InlineData(10, 200, 20)]
    [InlineData(25, 400, 100)]
    [InlineData(0, 500, 0)]
    [InlineData(100, 250, 250)]
    public void ApplyTo_Decimal_ReturnsRoundedProduct(decimal percent, decimal amount, decimal expected)
    {
        Percentage.Create(percent).ApplyTo(amount).ShouldBe(expected);
    }

    [Fact]
    public void ApplyTo_DecimalWithFractionalResult_RoundsToTwoDecimals()
    {
        Percentage.Create(33).ApplyTo(100m).ShouldBe(33m);
    }

    [Fact]
    public void ApplyTo_Money_ReturnsMoneyReducedByThePercentage()
    {
        var money = Money.Create(200m);

        var applied = Percentage.Create(25).ApplyTo(money);

        applied.Amount.ShouldBe(50m);
        applied.Currency.ShouldBe(money.Currency);
    }

    [Fact]
    public void ApplyTo_MoneyWithZeroPercent_ReturnsZeroAmountInSameCurrency()
    {
        var money = Money.Create(200m, "IRR");

        var applied = Percentage.Create(0).ApplyTo(money);

        applied.Amount.ShouldBe(0m);
        applied.Currency.ShouldBe("IRR");
    }

    [Fact]
    public void ToString_FormatsWithPercentSignAndTwoDecimals()
    {
        var s = Percentage.Create(12.5m).ToString();

        s.ShouldContain("%");
        s.ShouldContain("12");
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        Percentage.Create(30).ShouldBe(Percentage.Create(30));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        Percentage.Create(30).ShouldNotBe(Percentage.Create(31));
    }
}
