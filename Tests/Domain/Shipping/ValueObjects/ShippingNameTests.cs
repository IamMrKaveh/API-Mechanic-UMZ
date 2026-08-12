using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Shipping.ValueObjects;

public class ShippingNameTests
{
    [Fact]
    public void Create_WithValidName_PreservesOriginalValue()
    {
        var sut = ShippingName.Create("پست پیشتاز");

        sut.Value.ShouldBe("پست پیشتاز");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => ShippingName.Create(input!));
    }

    [Fact]
    public void Create_WithSingleCharacter_ThrowsDomainExceptionForMinimumLength()
    {
        Should.Throw<DomainException>(() => ShippingName.Create("x"))
            .Message.ShouldContain("2");
    }

    [Fact]
    public void Create_WithLengthExactlyAtMinimum_Succeeds()
    {
        Should.NotThrow(() => ShippingName.Create("ab"));
    }

    [Fact]
    public void Create_WithLengthExactlyAtMaximum_Succeeds()
    {
        var name = new string('x', ShippingName.MaxLength);

        Should.NotThrow(() => ShippingName.Create(name));
    }

    [Fact]
    public void Create_WithLengthExceedingMaximum_ThrowsDomainException()
    {
        var name = new string('x', ShippingName.MaxLength + 1);

        Should.Throw<DomainException>(() => ShippingName.Create(name))
            .Message.ShouldContain(ShippingName.MaxLength.ToString());
    }

    [Fact]
    public void Equality_ForSameValueDifferentCasing_TreatsInstancesAsEqual()
    {
        ShippingName.Create("Tipax").ShouldBe(ShippingName.Create("TIPAX"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsNotEqual()
    {
        ShippingName.Create("Tipax").ShouldNotBe(ShippingName.Create("Post"));
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsUnderlyingValue()
    {
        var sut = ShippingName.Create("Tipax");

        string extracted = sut;

        extracted.ShouldBe("Tipax");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        ShippingName.Create("Tipax").ToString().ShouldBe("Tipax");
    }
}
