namespace Tests.DomainTest.Product.ValueObjects;

public class ProductNameTests
{
    [Theory]
    [InlineData("لپ‌تاپ")]
    [InlineData("iPhone 14 Pro Max")]
    [InlineData("Samsung Galaxy S23")]
    public void Create_WithValidName_ShouldSucceed(string name)
    {
        var result = ProductName.Create(name);

        result.Should().NotBeNull();
        result.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithNullOrEmpty_ShouldThrowDomainException(string name)
    {
        var act = () => ProductName.Create(name);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithSingleCharacter_ShouldThrowDomainException()
    {
        var act = () => ProductName.Create("A");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ShouldThrowDomainException()
    {
        var longName = new string('A', 101);

        var act = () => ProductName.Create(longName);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithExactlyMaxLength_ShouldSucceed()
    {
        var maxLengthName = new string('A', ProductName.MaxLength);

        var result = ProductName.Create(maxLengthName);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        var result = ProductName.Create("  لپ‌تاپ  ");

        result.Value.Should().Be("لپ‌تاپ");
    }

    [Fact]
    public void Create_ShouldNormalizeArabicYe()
    {
        var result = ProductName.Create("كمپيوتر");

        result.Value.Should().NotContain("ي");
        result.Value.Should().NotContain("ك");
    }

    [Fact]
    public void TwoProductNames_WithSameValue_ShouldBeEqual()
    {
        var name1 = ProductName.Create("لپ‌تاپ");
        var name2 = ProductName.Create("لپ‌تاپ");

        name1.Should().Be(name2);
    }

    [Fact]
    public void TwoProductNames_WithDifferentCase_ShouldBeEqual()
    {
        var name1 = ProductName.Create("laptop");
        var name2 = ProductName.Create("LAPTOP");

        name1.Should().Be(name2);
    }

    [Fact]
    public void ImplicitConversion_ToStringOperator_ShouldReturnValue()
    {
        var name = ProductName.Create("لپ‌تاپ");

        string result = name;

        result.Should().Be(name.Value);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var name = ProductName.Create("لپ‌تاپ");

        name.ToString().Should().Be(name.Value);
    }
}