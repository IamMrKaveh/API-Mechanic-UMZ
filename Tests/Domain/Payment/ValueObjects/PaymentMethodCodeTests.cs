using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Payment.ValueObjects;

public class PaymentMethodCodeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() => PaymentMethodCode.Create(value!));
    }

    [Fact]
    public void Create_NormalizesToLowercase()
    {
        PaymentMethodCode.Create("ZARINPAL").Value.ShouldBe("zarinpal");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        PaymentMethodCode.Create("  cash-on-delivery  ").Value.ShouldBe("cash-on-delivery");
    }

    [Theory]
    [InlineData("zarinpal")]
    [InlineData("cash-on-delivery")]
    [InlineData("wallet")]
    [InlineData("pm-123")]
    [InlineData("a1")]
    public void Create_WithAllowedCharacters_ReturnsCode(string value)
    {
        PaymentMethodCode.Create(value).Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("ZarinPal Sandbox")]
    [InlineData("zarin_pal")]
    [InlineData("zarin.pal")]
    [InlineData("zarin/pal")]
    [InlineData("پرداخت")]
    public void Create_WithDisallowedCharacters_ThrowsDomainException(string value)
    {
        Should.Throw<DomainException>(() => PaymentMethodCode.Create(value));
    }

    [Fact]
    public void Create_ExceedingMaxLength_ThrowsDomainException()
    {
        var value = new string('a', PaymentMethodCode.MaxLength + 1);

        Should.Throw<DomainException>(() => PaymentMethodCode.Create(value));
    }

    [Fact]
    public void IsWallet_ForWalletValue_ReturnsTrue()
    {
        PaymentMethodCode.Create(PaymentMethodCode.Wallet).IsWallet.ShouldBeTrue();
    }

    [Fact]
    public void IsWallet_ForNonWalletValue_ReturnsFalse()
    {
        PaymentMethodCode.Create(PaymentMethodCode.Zarinpal).IsWallet.ShouldBeFalse();
    }

    [Fact]
    public void IsCashOnDelivery_ForCashOnDeliveryValue_ReturnsTrue()
    {
        PaymentMethodCode.Create(PaymentMethodCode.CashOnDelivery).IsCashOnDelivery.ShouldBeTrue();
    }

    [Theory]
    [InlineData("zarinpal", true)]
    [InlineData("zarinpal-sandbox", true)]
    [InlineData("wallet", false)]
    [InlineData("cash-on-delivery", false)]
    public void IsOnlineGateway_ReflectsCodeType(string value, bool expected)
    {
        PaymentMethodCode.Create(value).IsOnlineGateway.ShouldBe(expected);
    }

    [Fact]
    public void Equality_ByNormalizedValue()
    {
        PaymentMethodCode.Create("Zarinpal").ShouldBe(PaymentMethodCode.Create("zarinpal"));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsNormalizedValue()
    {
        string s = PaymentMethodCode.Create("ZARINPAL");

        s.ShouldBe("zarinpal");
    }
}
