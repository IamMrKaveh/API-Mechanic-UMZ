using Domain.Brand.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Brand.ValueObjects;

public class BrandNameTests
{
    [Theory]
    [InlineData("Ok")]
    [InlineData("Nike")]
    [InlineData("Bank Melli")]
    [InlineData("سامسونگ")]
    public void Create_WithValidLength_ReturnsBrandNameWithTrimmedValue(string input)
    {
        BrandName.Create(input).Value.ShouldBe(input);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsBeforeStoring()
    {
        BrandName.Create("  Adidas  ").Value.ShouldBe("Adidas");
    }

    [Fact]
    public void Create_AtExactlyMinLength_Succeeds()
    {
        BrandName.Create("Ab").Value.Length.ShouldBe(2);
    }

    [Fact]
    public void Create_AtExactlyMaxLength_Succeeds()
    {
        var input = new string('a', BrandName.MaxLength);

        BrandName.Create(input).Value.Length.ShouldBe(BrandName.MaxLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => BrandName.Create(input!));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("x")]
    [InlineData(" a ")]
    public void Create_WithLengthBelowMinAfterTrimming_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => BrandName.Create(input));
    }

    [Fact]
    public void Create_WithLengthOneAboveMax_ThrowsDomainException()
    {
        var input = new string('a', BrandName.MaxLength + 1);

        Should.Throw<DomainException>(() => BrandName.Create(input));
    }

    [Fact]
    public void MaxLength_IsOneHundred()
    {
        BrandName.MaxLength.ShouldBe(100);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        BrandName.Create("Nike").ToString().ShouldBe("Nike");
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        BrandName.Create("Nike").ShouldBe(BrandName.Create("Nike"));
    }

    [Fact]
    public void Equality_ForRecordWithDifferentValue_TreatsInstancesAsUnequal()
    {
        BrandName.Create("Nike").ShouldNotBe(BrandName.Create("Adidas"));
    }
}
