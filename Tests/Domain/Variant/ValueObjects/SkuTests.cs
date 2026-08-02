using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace Tests.Domain.Variant.ValueObjects;

public class SkuTests
{
    [Theory]
    [InlineData("ABC123")]
    [InlineData("A")]
    [InlineData("A-1_2.3")]
    public void Create_WithValidValue_ReturnsSku(string input)
    {
        Sku.Create(input).Value.ShouldBe(input);
    }

    [Fact]
    public void Create_UppercasesLowercaseValue()
    {
        Sku.Create("abc-123").Value.ShouldBe("ABC-123");
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        Sku.Create("  ABC-123  ").Value.ShouldBe("ABC-123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => Sku.Create(input));
    }

    [Fact]
    public void Create_WithNull_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => Sku.Create(null!));
    }

    [Fact]
    public void Create_AtMaxLength_Succeeds()
    {
        var input = new string('A', 100);

        Sku.Create(input).Value.Length.ShouldBe(100);
    }

    [Fact]
    public void Create_AboveMaxLength_ThrowsDomainException()
    {
        var input = new string('A', 101);

        Should.Throw<DomainException>(() => Sku.Create(input));
    }

    [Theory]
    [InlineData("ABC 123")]
    [InlineData("ABC@123")]
    [InlineData("ABC/123")]
    [InlineData("ABC#")]
    [InlineData("ABC+")]
    public void Create_WithForbiddenCharacter_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => Sku.Create(input));
    }

    [Fact]
    public void Equality_IsCaseInsensitiveViaGetEqualityComponents()
    {
        Sku.Create("abc-123").ShouldBe(Sku.Create("ABC-123"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        Sku.Create("ABC-123").ShouldNotBe(Sku.Create("XYZ-999"));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = Sku.Create("abc-123");

        s.ShouldBe("ABC-123");
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        Sku.Create("ABC").ShouldBeAssignableTo<ValueObject>();
    }
}
