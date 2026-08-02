using Domain.Product.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Product.ValueObjects;

public class ProductSlugTests
{
    [Fact]
    public void Create_WithValidDisplayName_ReturnsProductSlugInstance()
    {
        var sut = ProductSlug.Create("Nike Air Max");

        sut.ShouldBeOfType<ProductSlug>();
        sut.Value.ShouldBe("nike-air-max");
    }

    [Fact]
    public void Create_ReturnsInstanceAssignableToParentSlug()
    {
        ProductSlug.Create("nike").ShouldBeAssignableTo<Slug>();
    }

    [Fact]
    public void FromString_WithAlreadyNormalizedSlug_ReturnsProductSlugInstance()
    {
        var sut = ProductSlug.FromString("nike-air");

        sut.ShouldBeOfType<ProductSlug>();
        sut.Value.ShouldBe("nike-air");
    }

    [Fact]
    public void FromString_WithUppercaseInput_LowercasesValue()
    {
        ProductSlug.FromString("NIKE-AIR").Value.ShouldBe("nike-air");
    }

    [Fact]
    public void GenerateFrom_WithDisplayName_ReturnsNormalizedProductSlug()
    {
        var sut = ProductSlug.GenerateFrom("Nike Air Max 90");

        sut.ShouldBeOfType<ProductSlug>();
        sut.Value.ShouldBe("nike-air-max-90");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => ProductSlug.Create(input));
    }

    [Fact]
    public void FromString_WithInvalidCharacters_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => ProductSlug.FromString("nike air"));
    }

    [Fact]
    public void Equality_ForValueObjectWithSameValue_TreatsInstancesAsEqual()
    {
        ProductSlug.Create("nike").ShouldBe(ProductSlug.Create("nike"));
    }
}
