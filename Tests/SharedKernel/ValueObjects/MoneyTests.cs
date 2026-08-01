using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithNoCurrency_DefaultsToIRT()
    {
        var sut = Money.Create(100m);

        sut.Amount.ShouldBe(100m);
        sut.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void Create_WithLowercaseCurrency_NormalizesToUppercase()
    {
        var sut = Money.Create(100m, "irr");

        sut.Currency.ShouldBe("IRR");
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceCurrency_TrimsAfterUppercasing()
    {
        var sut = Money.Create(100m, "  usd  ");

        sut.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_WithZeroAmount_ReturnsMoneyWithZero()
    {
        Money.Create(0m).Amount.ShouldBe(0m);
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Money.Create(-1m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceCurrency_ThrowsArgumentException(string currency)
    {
        Should.Throw<ArgumentException>(() => Money.Create(1m, currency));
    }

    [Fact]
    public void FromDecimal_BehavesIdenticallyToCreate()
    {
        var a = Money.FromDecimal(50m, "USD");
        var b = Money.Create(50m, "USD");

        a.ShouldBe(b);
    }

    [Fact]
    public void Zero_WithDefaultCurrency_ReturnsIrtZero()
    {
        var sut = Money.Zero();

        sut.Amount.ShouldBe(0m);
        sut.Currency.ShouldBe("IRT");
        sut.IsZero().ShouldBeTrue();
    }

    [Fact]
    public void Zero_WithExplicitCurrency_ReturnsZeroInThatCurrency()
    {
        Money.Zero("IRR").Currency.ShouldBe("IRR");
    }

    [Fact]
    public void Copy_ReturnsEqualButDifferentInstance()
    {
        var original = Money.Create(100m, "USD");

        var copy = original.Copy();

        copy.ShouldBe(original);
        copy.ShouldNotBeSameAs(original);
    }

    [Fact]
    public void Add_TwoMoneyInSameCurrency_ReturnsSumInThatCurrency()
    {
        var sum = Money.Create(30m, "IRT").Add(Money.Create(70m, "IRT"));

        sum.Amount.ShouldBe(100m);
        sum.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void Add_TwoMoneyInDifferentCurrencies_ThrowsInvalidOperation()
    {
        var a = Money.Create(30m, "IRT");
        var b = Money.Create(70m, "USD");

        Should.Throw<InvalidOperationException>(() => a.Add(b));
    }

    [Fact]
    public void Subtract_TwoMoneyInSameCurrency_ReturnsDifference()
    {
        var diff = Money.Create(100m).Subtract(Money.Create(40m));

        diff.Amount.ShouldBe(60m);
    }

    [Fact]
    public void Subtract_WhenLhsSmallerThanRhs_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(
            () => Money.Create(10m).Subtract(Money.Create(20m)));
    }

    [Fact]
    public void Subtract_TwoMoneyInDifferentCurrencies_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(
            () => Money.Create(30m, "IRT").Subtract(Money.Create(5m, "USD")));
    }

    [Theory]
    [InlineData(100, 1.5, 150)]
    [InlineData(100, 0, 0)]
    [InlineData(50, 2, 100)]
    public void Multiply_ByDecimalFactor_ReturnsRoundedProduct(
        decimal amount, decimal factor, decimal expected)
    {
        Money.Create(amount).Multiply(factor).Amount.ShouldBe(expected);
    }

    [Fact]
    public void Multiply_ByNegativeDecimalFactor_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Money.Create(10m).Multiply(-1m));
    }

    [Theory]
    [InlineData(100, 2, 200)]
    [InlineData(100, 0, 0)]
    public void Multiply_ByIntFactor_ReturnsRoundedProduct(
        decimal amount, int factor, decimal expected)
    {
        Money.Create(amount).Multiply(factor).Amount.ShouldBe(expected);
    }

    [Fact]
    public void Multiply_ByNegativeIntFactor_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Money.Create(10m).Multiply(-1));
    }

    [Fact]
    public void Multiply_ByDecimalFactor_RoundsToTwoDecimalPlaces()
    {
        var sut = Money.Create(3m).Multiply(0.3333m);

        sut.Amount.ShouldBe(1.00m);
    }

    [Fact]
    public void IsGreaterThan_LhsBiggerInSameCurrency_ReturnsTrue()
    {
        Money.Create(20m).IsGreaterThan(Money.Create(10m)).ShouldBeTrue();
    }

    [Fact]
    public void IsGreaterThan_LhsEqualInSameCurrency_ReturnsFalse()
    {
        Money.Create(20m).IsGreaterThan(Money.Create(20m)).ShouldBeFalse();
    }

    [Fact]
    public void IsGreaterThanOrEqual_LhsEqualInSameCurrency_ReturnsTrue()
    {
        Money.Create(20m).IsGreaterThanOrEqual(Money.Create(20m)).ShouldBeTrue();
    }

    [Fact]
    public void IsLessThan_LhsSmallerInSameCurrency_ReturnsTrue()
    {
        Money.Create(5m).IsLessThan(Money.Create(10m)).ShouldBeTrue();
    }

    [Fact]
    public void IsLessThanOrEqual_LhsEqualInSameCurrency_ReturnsTrue()
    {
        Money.Create(5m).IsLessThanOrEqual(Money.Create(5m)).ShouldBeTrue();
    }

    [Fact]
    public void IsGreaterThan_DifferentCurrencies_ThrowsInvalidOperation()
    {
        Should.Throw<InvalidOperationException>(
            () => Money.Create(5m, "IRT").IsGreaterThan(Money.Create(1m, "USD")));
    }

    [Fact]
    public void IsZero_OnZeroAmount_ReturnsTrue()
    {
        Money.Zero().IsZero().ShouldBeTrue();
    }

    [Fact]
    public void IsZero_OnNonZeroAmount_ReturnsFalse()
    {
        Money.Create(1m).IsZero().ShouldBeFalse();
    }

    [Fact]
    public void ToTomanDecimal_WhenCurrencyIsRial_DividesByTen()
    {
        Money.Create(1000m, "IRR").ToTomanDecimal().ShouldBe(100m);
    }

    [Fact]
    public void ToTomanDecimal_WhenCurrencyIsToman_ReturnsAmountUnchanged()
    {
        Money.Create(1000m, "IRT").ToTomanDecimal().ShouldBe(1000m);
    }

    [Fact]
    public void ToRialDecimal_WhenCurrencyIsToman_MultipliesByTen()
    {
        Money.Create(100m, "IRT").ToRialDecimal().ShouldBe(1000m);
    }

    [Fact]
    public void ToRialDecimal_WhenCurrencyIsRial_ReturnsAmountUnchanged()
    {
        Money.Create(1000m, "IRR").ToRialDecimal().ShouldBe(1000m);
    }

    [Fact]
    public void ToTomanString_ContainsTomanLabel()
    {
        Money.Create(1000m, "IRT").ToTomanString().ShouldContain("تومان");
    }

    [Fact]
    public void ToRialString_ContainsRialLabel()
    {
        Money.Create(1000m, "IRR").ToRialString().ShouldContain("ریال");
    }

    [Fact]
    public void Equality_ForValueObjectWithSameAmountAndCurrency_TreatsInstancesAsEqual()
    {
        Money.Create(50m, "USD").ShouldBe(Money.Create(50m, "USD"));
    }

    [Fact]
    public void Equality_ForValueObjectWithSameAmountButDifferentCurrency_TreatsInstancesAsUnequal()
    {
        Money.Create(50m, "USD").ShouldNotBe(Money.Create(50m, "IRT"));
    }
}
