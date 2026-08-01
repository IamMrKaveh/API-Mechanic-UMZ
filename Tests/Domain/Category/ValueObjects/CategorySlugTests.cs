using Domain.Category.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Category.ValueObjects;

public class CategorySlugTests
{
    [Fact]
    public void Create_WithValidDisplayName_ReturnsCategorySlugInstance()
    {
        var sut = CategorySlug.Create("Home Appliances");

        sut.ShouldBeOfType<CategorySlug>();
        sut.Value.ShouldBe("home-appliances");
    }

    [Fact]
    public void Create_ReturnsInstanceAssignableToParentSlug()
    {
        CategorySlug.Create("electronics").ShouldBeAssignableTo<Slug>();
    }

    [Fact]
    public void FromString_WithAlreadyNormalizedSlug_ReturnsCategorySlugInstance()
    {
        var sut = CategorySlug.FromString("electronics");

        sut.ShouldBeOfType<CategorySlug>();
        sut.Value.ShouldBe("electronics");
    }

    [Fact]
    public void FromString_WithUppercaseInput_LowercasesValue()
    {
        CategorySlug.FromString("ELECTRONICS").Value.ShouldBe("electronics");
    }

    [Fact]
    public void GenerateFrom_WithDisplayName_ReturnsNormalizedCategorySlug()
    {
        var sut = CategorySlug.GenerateFrom("Home Appliances");

        sut.ShouldBeOfType<CategorySlug>();
        sut.Value.ShouldBe("home-appliances");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => CategorySlug.Create(input));
    }

    [Fact]
    public void FromString_WithInvalidSlugCharacters_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => CategorySlug.FromString("home appliances"));
    }

    [Fact]
    public void Equality_ForValueObjectWithSameValue_TreatsInstancesAsEqual() => CategorySlug.Create("electronics").ShouldBe(CategorySlug.Create("electronics"));
}
