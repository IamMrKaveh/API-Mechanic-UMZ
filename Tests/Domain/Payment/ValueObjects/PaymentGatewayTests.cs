using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Payment.ValueObjects;

public class PaymentGatewayTests
{
    [Theory]
    [InlineData("Zarinpal", true)]
    [InlineData("Mellat", true)]
    [InlineData("Saman", true)]
    [InlineData("Parsian", true)]
    [InlineData("Pasargad", true)]
    [InlineData("Wallet", true)]
    [InlineData("Saderat", false)]
    public void KnownStaticGateways_HaveExpectedIsActiveFlag(string value, bool expectedActive)
    {
        var sut = PaymentGateway.FromString(value);

        sut.Value.ShouldBe(value);
        sut.IsActive.ShouldBe(expectedActive);
    }

    [Theory]
    [InlineData("zarinpal", "Zarinpal")]
    [InlineData("MELLAT", "Mellat")]
    [InlineData("SaMaN", "Saman")]
    [InlineData("parsian", "Parsian")]
    [InlineData("pasargad", "Pasargad")]
    [InlineData("saderat", "Saderat")]
    [InlineData("wallet", "Wallet")]
    public void FromString_IsCaseInsensitive_ForKnownGateways(string input, string expectedValue)
    {
        PaymentGateway.FromString(input).Value.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromString_WithNullOrWhitespace_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() => PaymentGateway.FromString(value!));
    }

    [Fact]
    public void FromString_WithUnknownValue_ReturnsCustomGatewayWithSameValue()
    {
        var sut = PaymentGateway.FromString("StripeEU");

        sut.Value.ShouldBe("StripeEU");
        sut.DisplayName.ShouldBe("StripeEU");
        sut.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Custom_WithDisplayName_UsesProvidedDisplayName()
    {
        var sut = PaymentGateway.Custom("Stripe", "Stripe Global");

        sut.Value.ShouldBe("Stripe");
        sut.DisplayName.ShouldBe("Stripe Global");
    }

    [Fact]
    public void Custom_TrimsSurroundingWhitespaceOfValue()
    {
        PaymentGateway.Custom("  Stripe  ").Value.ShouldBe("Stripe");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Custom_WithNullOrWhitespace_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() => PaymentGateway.Custom(value!));
    }

    [Fact]
    public void Equality_ByValueIsCaseInsensitive()
    {
        PaymentGateway.FromString("Zarinpal").ShouldBe(PaymentGateway.FromString("zarinpal"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        PaymentGateway.FromString("Zarinpal").ShouldNotBe(PaymentGateway.FromString("Mellat"));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = PaymentGateway.FromString("Zarinpal");

        s.ShouldBe("Zarinpal");
    }
}
