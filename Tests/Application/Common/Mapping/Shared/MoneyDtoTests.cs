using Application.Common.Mapping.Shared;

namespace Tests.Application.Common.Mapping.Shared;

public class MoneyDtoTests
{
    [Fact]
    public void Constructor_AssignsAmountAndCurrency()
    {
        var sut = new MoneyDto(1500m, "IRT");

        sut.Amount.ShouldBe(1500m);
        sut.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void Equality_WhenSameAmountAndCurrency_AreEqual()
    {
        var a = new MoneyDto(99.9m, "USD");
        var b = new MoneyDto(99.9m, "USD");

        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equality_WhenDifferentAmount_AreNotEqual()
    {
        var a = new MoneyDto(100m, "IRT");
        var b = new MoneyDto(200m, "IRT");

        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_WhenDifferentCurrency_AreNotEqual()
    {
        var a = new MoneyDto(100m, "IRT");
        var b = new MoneyDto(100m, "USD");

        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void PercentageDto_AssignsValue()
    {
        var sut = new PercentageDto(15.5m);

        sut.Value.ShouldBe(15.5m);
    }

    [Fact]
    public void PercentageDto_Equality_WhenSameValue_AreEqual()
    {
        var a = new PercentageDto(10m);
        var b = new PercentageDto(10m);

        (a == b).ShouldBeTrue();
    }
}
