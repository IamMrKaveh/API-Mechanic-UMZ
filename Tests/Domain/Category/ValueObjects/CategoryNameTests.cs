using Domain.Category.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace Tests.Domain.Category.ValueObjects;

public class CategoryNameTests
{
    [Theory]
    [InlineData("Ok")]
    [InlineData("Electronics")]
    [InlineData("لوازم خانگی")]
    public void Create_WithValidLength_ReturnsCategoryNameWithTrimmedValue(string input)
    {
        CategoryName.Create(input).Value.ShouldBe(input);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsBeforeStoring()
    {
        CategoryName.Create("  Books  ").Value.ShouldBe("Books");
    }

    [Fact]
    public void Create_AtExactlyMinLength_Succeeds()
    {
        CategoryName.Create("Ab").Value.Length.ShouldBe(2);
    }

    [Fact]
    public void Create_AtExactlyMaxLength_Succeeds()
    {
        var input = new string('a', CategoryName.MaxLength);

        CategoryName.Create(input).Value.Length.ShouldBe(CategoryName.MaxLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => CategoryName.Create(input!));
    }

    [Theory]
    [InlineData("a")]
    [InlineData(" b ")]
    public void Create_WithLengthBelowMinAfterTrimming_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => CategoryName.Create(input));
    }

    [Fact]
    public void Create_WithLengthOneAboveMax_ThrowsDomainException()
    {
        var input = new string('a', CategoryName.MaxLength + 1);

        Should.Throw<DomainException>(() => CategoryName.Create(input));
    }

    [Fact]
    public void MaxLength_IsOneHundred()
    {
        CategoryName.MaxLength.ShouldBe(100);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        CategoryName.Create("Books").ToString().ShouldBe("Books");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = CategoryName.Create("Books");

        s.ShouldBe("Books");
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        CategoryName.Create("Books").ShouldBe(CategoryName.Create("BOOKS"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        CategoryName.Create("Books").ShouldNotBe(CategoryName.Create("Movies"));
    }

    [Fact]
    public void GetHashCode_IsCaseInsensitive()
    {
        CategoryName.Create("Books").GetHashCode().ShouldBe(CategoryName.Create("books").GetHashCode());
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        CategoryName.Create("Books").ShouldBeAssignableTo<ValueObject>();
    }
}
