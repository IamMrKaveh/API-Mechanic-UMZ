using Domain.Payment.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Payment.ValueObjects;

public class PaymentAuthorityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? value)
    {
        Should.Throw<DomainException>(() => PaymentAuthority.Create(value!));
    }

    [Fact]
    public void Create_WithFewerThanMinLength_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => PaymentAuthority.Create("abcd"));
    }

    [Fact]
    public void Create_WithExactMinLength_ReturnsAuthority()
    {
        PaymentAuthority.Create("abcde").Value.ShouldBe("abcde");
    }

    [Fact]
    public void Create_WithMoreThanMaxLength_ThrowsDomainException()
    {
        var value = new string('a', 201);

        Should.Throw<DomainException>(() => PaymentAuthority.Create(value));
    }

    [Fact]
    public void Create_WithExactMaxLength_ReturnsAuthority()
    {
        var value = new string('a', 200);

        PaymentAuthority.Create(value).Value.ShouldBe(value);
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        PaymentAuthority.Create("  A12345  ").Value.ShouldBe("A12345");
    }

    [Fact]
    public void Equality_ForSameNormalizedValue_TreatsInstancesAsEqual()
    {
        PaymentAuthority.Create("AUTH-12345").ShouldBe(PaymentAuthority.Create("AUTH-12345"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        PaymentAuthority.Create("AUTH-12345").ShouldNotBe(PaymentAuthority.Create("AUTH-99999"));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = PaymentAuthority.Create("AUTH-12345");

        s.ShouldBe("AUTH-12345");
    }
}
