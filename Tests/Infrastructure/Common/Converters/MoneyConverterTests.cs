using Infrastructure.Common.Converters;
using SharedKernel.ValueObjects;

namespace Tests.Infrastructure.Common.Converters;

public class MoneyConverterTests
{
    [Fact]
    public void ConvertToProvider_WithMoneyValue_ReturnsUnderlyingAmount()
    {
        var sut = new MoneyConverter(); var money = Money.Create(1500m, "IRT");

        var provider = sut.ConvertToProvider(money);

        provider.ShouldBe(1500m);
    }

    [Fact]
    public void ConvertToProvider_WithZeroMoney_ReturnsZeroDecimal()
    {
        var sut = new MoneyConverter();
        var money = Money.Zero();

        var provider = sut.ConvertToProvider(money);

        provider.ShouldBe(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2500)]
    [InlineData(1234567890)]
    public void ConvertFromProvider_WithNonNegativeDecimal_ReturnsMoneyWithIrtCurrencyAndSameAmount(decimal amount)
    {
        var sut = new MoneyConverter();

        var model = sut.ConvertFromProvider(amount);

        model.ShouldBeOfType<Money>();
        var money = (Money)model!;
        money.Amount.ShouldBe(amount);
        money.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void ConvertFromProvider_WithNegativeDecimal_ThrowsArgumentException()
    {
        var sut = new MoneyConverter();

        Should.Throw<ArgumentException>(() => sut.ConvertFromProvider(-1m));
    }

    [Fact]
    public void Roundtrip_ConvertToThenFromProvider_YieldsEqualMoneyInIrt()
    {
        var sut = new MoneyConverter();
        var original = Money.Create(9999.99m, "IRT");

        var providerValue = sut.ConvertToProvider(original);
        var restored = sut.ConvertFromProvider(providerValue);

        restored.ShouldBe(original);
    }

    [Fact]
    public void ConvertToProvider_WithNonIrtMoney_StillReturnsOnlyAmountLosingCurrency()
    {
        var sut = new MoneyConverter();
        var money = Money.Create(500m, "USD");

        var provider = sut.ConvertToProvider(money);

        provider.ShouldBe(500m);
    }

    [Fact]
    public void ProviderClrType_IsDecimal()
    {
        var sut = new MoneyConverter();

        sut.ProviderClrType.ShouldBe(typeof(decimal));
    }

    [Fact]
    public void ModelClrType_IsMoney()
    {
        var sut = new MoneyConverter();

        sut.ModelClrType.ShouldBe(typeof(Money));
    }
}
