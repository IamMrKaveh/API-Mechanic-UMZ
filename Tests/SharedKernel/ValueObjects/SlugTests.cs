using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class SlugTests
{
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  hello  ", "hello")]
    [InlineData("hello.world", "hello-world")]
    [InlineData("hello_world", "hello-world")]
    [InlineData("hello   world", "hello-world")]
    public void Create_WithLatinInput_LowercasesAndNormalizesSeparators(string input, string expected)
    {
        Slug.Create(input).Value.ShouldBe(expected);
    }

    [Fact]
    public void Create_WithPersianInput_KeepsPersianCharactersAndReplacesSpaces()
    {
        Slug.Create("سلام دنیا").Value.ShouldBe("سلام-دنیا");
    }

    [Fact]
    public void Create_WithZeroWidthNonJoiner_ReplacesItWithHyphen()
    {
        Slug.Create("می‌شود").Value.ShouldBe("می-شود");
    }

    [Fact]
    public void Create_WithLeadingAndTrailingHyphensInSource_TrimsThem()
    {
        Slug.Create("---hello---").Value.ShouldBe("hello");
    }

    [Fact]
    public void Create_WithConsecutiveSeparators_CollapsesToSingleHyphen()
    {
        Slug.Create("hello    world").Value.ShouldBe("hello-world");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => Slug.Create(input));
    }

    [Fact]
    public void Create_WithValueExceedingMaxLength_ThrowsDomainException()
    {
        var input = new string('a', Slug.MaxLength + 1);

        Should.Throw<DomainException>(() => Slug.Create(input));
    }

    [Fact]
    public void Create_WithValueAtExactlyMaxLength_Succeeds()
    {
        var input = new string('a', Slug.MaxLength);

        Slug.Create(input).Value.Length.ShouldBe(Slug.MaxLength);
    }

    [Fact]
    public void FromString_WithAlreadyValidSlug_ReturnsSlugUnchanged()
    {
        Slug.FromString("hello-world").Value.ShouldBe("hello-world");
    }

    [Fact]
    public void FromString_WithUppercaseSlug_LowercasesIt()
    {
        Slug.FromString("HELLO-WORLD").Value.ShouldBe("hello-world");
    }

    [Fact]
    public void FromString_WithInvalidCharacter_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => Slug.FromString("hello world"));
    }

    [Fact]
    public void FromString_WithLeadingHyphen_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => Slug.FromString("-hello"));
    }

    [Fact]
    public void FromString_WithTrailingHyphen_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => Slug.FromString("hello-"));
    }

    [Fact]
    public void FromString_WithConsecutiveHyphens_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => Slug.FromString("hello--world"));
    }

    [Fact]
    public void GenerateFrom_DisplayName_ProducesNormalizedSlug()
    {
        Slug.GenerateFrom("My Awesome Product").Value.ShouldBe("my-awesome-product");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateFrom_WithEmptyDisplayName_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => Slug.GenerateFrom(input));
    }

    [Fact]
    public void Matches_WhenOtherEqualsValueCaseInsensitive_ReturnsTrue()
    {
        Slug.Create("hello").Matches("HELLO").ShouldBeTrue();
    }

    [Fact]
    public void Matches_WhenOtherHasSurroundingWhitespace_ReturnsTrue()
    {
        Slug.Create("hello").Matches("  hello  ").ShouldBeTrue();
    }

    [Fact]
    public void Matches_WhenOtherIsDifferent_ReturnsFalse()
    {
        Slug.Create("hello").Matches("world").ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Matches_WithNullOrEmpty_ReturnsFalse(string? input)
    {
        Slug.Create("hello").Matches(input!).ShouldBeFalse();
    }

    [Fact]
    public void ToString_ReturnsSlugValue()
    {
        Slug.Create("hello-world").ToString().ShouldBe("hello-world");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsSlugValue()
    {
        string s = Slug.Create("hello");

        s.ShouldBe("hello");
    }

    [Fact]
    public void Equality_ForValueObjectWithSameValue_TreatsInstancesAsEqual()
    {
        Slug.Create("hello").ShouldBe(Slug.Create("hello"));
    }

    [Fact]
    public void Equality_ForValueObjectWithDifferentValue_TreatsInstancesAsUnequal()
    {
        Slug.Create("hello").ShouldNotBe(Slug.Create("world"));
    }
}
