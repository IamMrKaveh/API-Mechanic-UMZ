using Infrastructure.Localization.Resources;

namespace Tests.Infrastructure.Localization.Resources;

public class ErrorMessagesTests
{
    [Fact]
    public void FaAndEn_HaveIdenticalKeySets()
    {
        ErrorMessages.Fa.Keys.ShouldBe(ErrorMessages.En.Keys, ignoreOrder: true);
    }

    [Fact]
    public void FaAndEn_AllValuesAreNonEmpty()
    {
        ErrorMessages.Fa.Count.ShouldBeGreaterThan(0);
        foreach (var (key, value) in ErrorMessages.Fa)
            value.ShouldNotBeNullOrWhiteSpace($"Fa[{key}]");
        foreach (var (key, value) in ErrorMessages.En)
            value.ShouldNotBeNullOrWhiteSpace($"En[{key}]");
    }

    [Fact]
    public void Dictionaries_UseOrdinalComparison()
    {
        var fa = (Dictionary<string, string>)ErrorMessages.Fa;

        fa.Comparer.ShouldBe(StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("error.general.unexpected", "خطای غیرمنتظره‌ای رخ داده است.")]
    [InlineData("error.order.not_found", "سفارش یافت نشد.")]
    [InlineData("error.payment.invalid_amount", "مبلغ پرداخت نامعتبر است.")]
    [InlineData("error.wallet.insufficient_balance", "کیف پول موجودی کافی ندارد.")]
    [InlineData("error.inventory.insufficient_stock", "موجودی کافی نیست.")]
    public void Fa_ContainsExpectedMessages(string key, string expected)
    {
        ErrorMessages.Fa[key].ShouldBe(expected);
    }

    [Theory]
    [InlineData("error.general.unexpected", "An unexpected error has occurred.")]
    [InlineData("error.order.not_found", "Order not found.")]
    [InlineData("error.payment.invalid_amount", "The payment amount is invalid.")]
    [InlineData("error.wallet.insufficient_balance", "Wallet has insufficient balance.")]
    [InlineData("error.inventory.insufficient_stock", "Insufficient stock.")]
    public void En_ContainsExpectedMessages(string key, string expected)
    {
        ErrorMessages.En[key].ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ForCulture_BlankCulture_ReturnsFa(string? culture)
    {
        ErrorMessages.ForCulture(culture!).ShouldBeSameAs(ErrorMessages.Fa);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("EN")]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("En")]
    public void ForCulture_EnglishCultures_ReturnEn(string culture)
    {
        ErrorMessages.ForCulture(culture).ShouldBeSameAs(ErrorMessages.En);
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("fa-IR")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("ar")]
    public void ForCulture_OtherCultures_ReturnFa(string culture)
    {
        ErrorMessages.ForCulture(culture).ShouldBeSameAs(ErrorMessages.Fa);
    }
}
