using Domain.Security.ValueObjects;
using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace Tests.Domain.Security.ValueObjects;

public class OtpCodeTests
{
    [Fact]
    public void Create_WithValidSixDigitCode_ReturnsOtpCodeWithThatValue()
    {
        OtpCode.Create("123456").Value.ShouldBe("123456");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsBeforeValidating()
    {
        OtpCode.Create("  123456  ").Value.ShouldBe("123456");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => OtpCode.Create(input!));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("1")]
    public void Create_WithLengthNotSix_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => OtpCode.Create(input));
    }

    [Theory]
    [InlineData("12345a")]
    [InlineData("abcdef")]
    [InlineData("12 456")]
    [InlineData("12-456")]
    public void Create_WithNonDigitCharacter_ThrowsDomainException(string input)
    {
        Should.Throw<DomainException>(() => OtpCode.Create(input));
    }

    [Fact]
    public void LengthConstant_IsSix()
    {
        OtpCode.Length.ShouldBe(6);
    }

    [Fact]
    public void Generate_ProducesCodeWithExactlySixCharacters()
    {
        OtpCode.Generate().Value.Length.ShouldBe(6);
    }

    [Fact]
    public void Generate_ProducesCodeContainingOnlyDigits()
    {
        OtpCode.Generate().Value.ShouldAllBe(c => char.IsDigit(c));
    }

    [Fact]
    public void Matches_WithIdenticalRawCode_ReturnsTrue()
    {
        OtpCode.Create("135790").Matches("135790").ShouldBeTrue();
    }

    [Fact]
    public void Matches_WithSurroundingWhitespaceOnProvided_ReturnsTrueAfterTrim()
    {
        OtpCode.Create("135790").Matches("  135790  ").ShouldBeTrue();
    }

    [Fact]
    public void Matches_WithDifferentCode_ReturnsFalse()
    {
        OtpCode.Create("135790").Matches("246801").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Matches_WithNullOrWhitespaceProvided_ReturnsFalse(string? input)
    {
        OtpCode.Create("135790").Matches(input!).ShouldBeFalse();
    }

    [Fact]
    public void Matches_WithDifferentLengthProvided_ReturnsFalse()
    {
        OtpCode.Create("135790").Matches("12345").ShouldBeFalse();
    }

    [Fact]
    public void ToHash_ProducesDeterministicHashForSameCode()
    {
        var hashA = OtpCode.Create("135790").ToHash();
        var hashB = OtpCode.Create("135790").ToHash();

        hashA.ShouldBe(hashB);
    }

    [Fact]
    public void ToHash_ProducesDifferentHashForDifferentCode()
    {
        OtpCode.Create("135790").ToHash().ShouldNotBe(OtpCode.Create("246801").ToHash());
    }

    [Fact]
    public void ToHash_DoesNotReturnRawCode()
    {
        OtpCode.Create("135790").ToHash().ShouldNotContain("135790");
    }

    [Fact]
    public void MatchesHash_WithHashOfSameCode_ReturnsTrue()
    {
        var stored = OtpCode.Create("135790").ToHash();

        OtpCode.Create("135790").MatchesHash(stored).ShouldBeTrue();
    }

    [Fact]
    public void MatchesHash_WithHashOfDifferentCode_ReturnsFalse()
    {
        var stored = OtpCode.Create("135790").ToHash();

        OtpCode.Create("246801").MatchesHash(stored).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MatchesHash_WithNullOrWhitespaceStored_ReturnsFalse(string? stored)
    {
        OtpCode.Create("135790").MatchesHash(stored!).ShouldBeFalse();
    }

    [Fact]
    public void GetMasked_KeepsFirstAndLastDigitAndMasksMiddle()
    {
        OtpCode.Create("135790").GetMasked().ShouldBe("1****0");
    }

    [Fact]
    public void GetMasked_ResultHasSameLengthAsOriginal()
    {
        OtpCode.Create("135790").GetMasked().Length.ShouldBe(6);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        OtpCode.Create("135790").ToString().ShouldBe("135790");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = OtpCode.Create("135790");

        s.ShouldBe("135790");
    }

    [Fact]
    public void Equality_ForSameValue_TreatsInstancesAsEqual()
    {
        OtpCode.Create("135790").ShouldBe(OtpCode.Create("135790"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        OtpCode.Create("135790").ShouldNotBe(OtpCode.Create("246801"));
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        OtpCode.Create("135790").ShouldBeAssignableTo<ValueObject>();
    }
}
