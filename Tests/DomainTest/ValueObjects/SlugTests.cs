namespace Tests.DomainTest.ValueObjects;

public class SlugTests
{
    [Theory]
    [InlineData("product-name")]
    [InlineData("category-123")]
    [InlineData("brand-test-slug")]
    public void Create_WithValidSlug_ShouldSucceed(string value)
    {
        var slug = Slug.Create(value);

        slug.Should().NotBeNull();
        slug.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithUpperCaseLetters_ShouldNormalizeToLowerCase()
    {
        var slug = Slug.Create("Product-Name");

        slug.Value.Should().Be("product-name");
    }

    [Fact]
    public void Create_WithSpaces_ShouldReplaceWithDashes()
    {
        var slug = Slug.Create("product name test");

        slug.Value.Should().NotContain(" ");
    }

    [Fact]
    public void Create_WithEmptyString_ShouldThrowDomainException()
    {
        var act = () => Slug.Create("");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNullValue_ShouldThrowDomainException()
    {
        var act = () => Slug.Create(null!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TwoSlugs_WithSameValue_ShouldBeEqual()
    {
        var a = Slug.Create("same-slug");
        var b = Slug.Create("same-slug");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoSlugs_WithDifferentValues_ShouldNotBeEqual()
    {
        var a = Slug.Create("slug-one");
        var b = Slug.Create("slug-two");

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ShouldReturnSlugValue()
    {
        var slug = Slug.Create("my-product");

        slug.ToString().Should().Be("my-product");
    }
}