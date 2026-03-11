namespace Tests.DomainTest.Discount;

public class DiscountCodeValueTests
{
    [Theory]
    [InlineData("ABC")]
    [InlineData("SAVE10")]
    [InlineData("BLACK-FRIDAY")]
    [InlineData("CODE_2025")]
    [InlineData("ABCDEFGHIJ1234567890")]
    public void Create_WithValidCode_ShouldSucceed(string code)
    {
        var result = DiscountCodeValue.Create(code);

        result.Should().NotBeNull();
        result.Value.Should().Be(code.ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithNullOrWhiteSpace_ShouldThrowDomainException(string code)
    {
        var act = () => DiscountCodeValue.Create(code);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("A")]
    public void Create_WithCodeShorterThanMinLength_ShouldThrowDomainException(string code)
    {
        var act = () => DiscountCodeValue.Create(code);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithCodeLongerThanMaxLength_ShouldThrowDomainException()
    {
        var longCode = new string('A', 21);

        var act = () => DiscountCodeValue.Create(longCode);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("CODE 10")]
    [InlineData("تخفیف")]
    [InlineData("CODE@10")]
    [InlineData("CODE#10")]
    public void Create_WithInvalidCharacters_ShouldThrowDomainException(string code)
    {
        var act = () => DiscountCodeValue.Create(code);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithLowercaseCode_ShouldNormalizeToUppercase()
    {
        var result = DiscountCodeValue.Create("save10");

        result.Value.Should().Be("SAVE10");
    }

    [Fact]
    public void TwoCodeValues_WithSameValue_ShouldBeEqual()
    {
        var code1 = DiscountCodeValue.Create("SAVE10");
        var code2 = DiscountCodeValue.Create("SAVE10");

        code1.Should().Be(code2);
    }

    [Fact]
    public void TwoCodeValues_WithDifferentValues_ShouldNotBeEqual()
    {
        var code1 = DiscountCodeValue.Create("SAVE10");
        var code2 = DiscountCodeValue.Create("SAVE20");

        code1.Should().NotBe(code2);
    }

    [Fact]
    public void ImplicitConversion_ToStringOperator_ShouldReturnValue()
    {
        var code = DiscountCodeValue.Create("SAVE10");

        string result = code;

        result.Should().Be("SAVE10");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var code = DiscountCodeValue.Create("SAVE10");

        code.ToString().Should().Be("SAVE10");
    }

    [Fact]
    public void FromPersistedString_ShouldCreateWithoutValidation()
    {
        var result = DiscountCodeValue.FromPersistedString("any-value");

        result.Should().NotBeNull();
        result.Value.Should().Be("any-value");
    }
}