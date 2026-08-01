using Domain.Brand.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Brand.ValueObjects;

public class BrandSlugTests
{
    [Fact]
    public void Create_WithValidDisplayName_ReturnsBrandSlugInstance()
    {
        var sut = BrandSlug.Create("Nike Sport");

        sut.ShouldBeOfType<BrandSlug>();
        sut.Value.ShouldBe("nike-sport");
    }

    [Fact]
    public void Create_ReturnsInstanceAssignableToParentSlug()
    {
        BrandSlug.Create("nike").ShouldBeAssignableTo<Slug>();
    }

    [Fact]
    public void FromString_WithAlreadyNormalizedSlug_ReturnsBrandSlugInstance()
    {
        var sut = BrandSlug.FromString("nike-sport");

        sut.ShouldBeOfType<BrandSlug>();
        sut.Value.ShouldBe("nike-sport");
    }

    [Fact]
    public void FromString_WithUppercaseInput_LowercasesValue()
    {
        BrandSlug.FromString("NIKE-SPORT").Value.ShouldBe("nike-sport");
    }

    [Fact]
    public void GenerateFrom_WithDisplayName_ReturnsBrandSlugInstanceWithNormalizedValue()
    {
        var sut = BrandSlug.GenerateFrom("My Awesome Brand");

        sut.ShouldBeOfType<BrandSlug>();
        sut.Value.ShouldBe("my-awesome-brand");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => BrandSlug.Create(input));
    }

    [Fact]
    public void FromString_WithInvalidSlugCharacters_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => BrandSlug.FromString("nike sport"));
    }

    [Fact]
    public void FromString_WithLeadingHyphen_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => BrandSlug.FromString("-nike"));
    }

    [Fact]
    public void Equality_ForValueObjectWithSameValue_TreatsInstancesAsEqual()
    {
        BrandSlug.Create("nike").ShouldBe(BrandSlug.Create("nike"));
    }

    [Fact]
    public void Equality_ForValueObjectWithDifferentValue_TreatsInstancesAsUnequal()
    {
        BrandSlug.Create("nike").ShouldNotBe(BrandSlug.Create("adidas"));
    }
}
