using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Payment.ValueObjects;

public class PaymentMethodNameTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsNameWithExactValue()
    {
        PaymentMethodName.Create("Zarinpal").Value.ShouldBe("Zarinpal");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() => PaymentMethodName.Create(value!));
    }

    [Fact]
    public void Create_WithSingleCharacter_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => PaymentMethodName.Create("Z"));
    }

    [Fact]
    public void Create_WithExactMinLength_ReturnsName()
    {
        PaymentMethodName.Create("Zp").Value.ShouldBe("Zp");
    }

    [Fact]
    public void Create_WithExactMaxLength_ReturnsName()
    {
        var value = new string('a', PaymentMethodName.MaxLength);

        PaymentMethodName.Create(value).Value.ShouldBe(value);
    }

    [Fact]
    public void Create_ExceedingMaxLength_ThrowsDomainException()
    {
        var value = new string('a', PaymentMethodName.MaxLength + 1);

        Should.Throw<DomainException>(() => PaymentMethodName.Create(value));
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        PaymentMethodName.Create("Zarinpal").ShouldBe(PaymentMethodName.Create("ZARINPAL"));
    }

    [Fact]
    public void ToString_ReturnsOriginalValue()
    {
        PaymentMethodName.Create("Zarinpal").ToString().ShouldBe("Zarinpal");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = PaymentMethodName.Create("Zarinpal");

        s.ShouldBe("Zarinpal");
    }
}
