using Domain.Product.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace Tests.Domain.Product.ValueObjects;

public class ProductNameTests
{
    [Theory]
    [InlineData("Ok")]
    [InlineData("Nike Air Max 90")]
    [InlineData("سامسونگ گلکسی")]
    public void Create_WithValidLength_ReturnsProductName(string input)
    {
        ProductName.Create(input).Value.ShouldBe(input);
    }

    [Fact]
    public void Create_DoesNotTrimSurroundingWhitespace()
    {
        var sut = ProductName.Create("  Nike  ");

        sut.Value.ShouldBe("  Nike  ");
    }

    [Fact]
    public void Create_AtExactlyMinLength_Succeeds()
    {
        ProductName.Create("Ab").Value.Length.ShouldBe(2);
    }

    [Fact]
    public void Create_AtExactlyMaxLength_Succeeds()
    {
        var input = new string('a', ProductName.MaxLength);

        ProductName.Create(input).Value.Length.ShouldBe(ProductName.MaxLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => ProductName.Create(input!));
    }

    [Fact]
    public void Create_WithSingleCharacter_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => ProductName.Create("a"));
    }

    [Fact]
    public void Create_WithLengthOneAboveMax_ThrowsDomainException()
    {
        var input = new string('a', ProductName.MaxLength + 1);

        Should.Throw<DomainException>(() => ProductName.Create(input));
    }

    [Fact]
    public void MaxLength_IsOneHundred()
    {
        ProductName.MaxLength.ShouldBe(100);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        ProductName.Create("Nike").ToString().ShouldBe("Nike");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = ProductName.Create("Nike");

        s.ShouldBe("Nike");
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        ProductName.Create("Nike").ShouldBe(ProductName.Create("NIKE"));
    }

    [Fact]
    public void Equality_WithSurroundingWhitespaceDifference_TreatsInstancesAsUnequal()
    {
        ProductName.Create("Nike").ShouldNotBe(ProductName.Create(" Nike "));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        ProductName.Create("Nike").ShouldNotBe(ProductName.Create("Adidas"));
    }

    [Fact]
    public void GetHashCode_IsCaseInsensitive()
    {
        ProductName.Create("Nike").GetHashCode().ShouldBe(ProductName.Create("nike").GetHashCode());
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        ProductName.Create("Nike").ShouldBeAssignableTo<ValueObject>();
    }
}
