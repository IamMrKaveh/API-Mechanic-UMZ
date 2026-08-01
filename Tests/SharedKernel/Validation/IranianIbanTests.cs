using SharedKernel.Validation;

namespace Tests.SharedKernel.Validation;

public class IranianIbanTests
{
    private const string ValidIbanAllZeros = "IR490000000000000000000000";
    private const string InvalidChecksumIban = "IR480000000000000000000000";

    [Fact]
    public void Constants_HaveExpectedValues()
    {
        IranianIban.TotalLength.ShouldBe(26);
        IranianIban.BodyDigitsLength.ShouldBe(24);
        IranianIban.CountryCode.ShouldBe("IR");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_WithNullOrWhitespace_ReturnsEmptyString(string? input)
    {
        IranianIban.Normalize(input).ShouldBe(string.Empty);
    }

    [Fact]
    public void Normalize_StripsWhitespaceAndDashesAndUnderscores()
    {
        IranianIban.Normalize("IR 49-0000_0000000000 0000000000").ShouldBe(ValidIbanAllZeros);
    }

    [Fact]
    public void Normalize_UppercasesLetters()
    {
        IranianIban.Normalize("ir490000000000000000000000").ShouldBe(ValidIbanAllZeros);
    }

    [Fact]
    public void Normalize_MapsPersianDigitsToAsciiDigits()
    {
        var persian = "IR۴۹۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰";

        IranianIban.Normalize(persian).ShouldBe(ValidIbanAllZeros);
    }

    [Fact]
    public void Normalize_MapsArabicIndicDigitsToAsciiDigits()
    {
        var arabic = "IR٤٩٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠٠";

        IranianIban.Normalize(arabic).ShouldBe(ValidIbanAllZeros);
    }

    [Fact]
    public void Normalize_WhenInput24DigitsWithoutPrefix_PrependsIrPrefix()
    {
        var body = new string('0', 24);

        IranianIban.Normalize(body).ShouldBe("IR" + body);
    }

    [Fact]
    public void Normalize_WhenInput24CharsWithLettersInMiddle_DoesNotPrependIr()
    {
        var mixed = "IR" + new string('0', 22);

        IranianIban.Normalize(mixed).Length.ShouldBe(24);
    }

    [Fact]
    public void HasValidFormat_WithValidLengthPrefixAndDigits_ReturnsTrue()
    {
        IranianIban.HasValidFormat(ValidIbanAllZeros).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("IR")]
    [InlineData("IR490000000000000000000")]
    [InlineData("IR4900000000000000000000000000")]
    public void HasValidFormat_WithWrongLength_ReturnsFalse(string input)
    {
        IranianIban.HasValidFormat(input).ShouldBeFalse();
    }

    [Fact]
    public void HasValidFormat_WithoutIrPrefix_ReturnsFalse()
    {
        IranianIban.HasValidFormat("US490000000000000000000000").ShouldBeFalse();
    }

    [Fact]
    public void HasValidFormat_WithNonDigitInBody_ReturnsFalse()
    {
        IranianIban.HasValidFormat("IR49000000000000000000000X").ShouldBeFalse();
    }

    [Fact]
    public void HasValidChecksum_ForKnownValidIban_ReturnsTrue()
    {
        IranianIban.HasValidChecksum(ValidIbanAllZeros).ShouldBeTrue();
    }

    [Fact]
    public void HasValidChecksum_ForIbanWithOffByOneCheckDigit_ReturnsFalse()
    {
        IranianIban.HasValidChecksum(InvalidChecksumIban).ShouldBeFalse();
    }

    [Fact]
    public void HasValidChecksum_ForAllZerosCheckDigits_ReturnsFalse()
    {
        IranianIban.HasValidChecksum("IR000000000000000000000000").ShouldBeFalse();
    }

    [Fact]
    public void HasValidChecksum_ForMalformedInput_ReturnsFalse()
    {
        IranianIban.HasValidChecksum("not-an-iban").ShouldBeFalse();
    }

    [Fact]
    public void TryParse_ForKnownValidInputInAsciiWithFormatting_NormalizesAndReturnsTrue()
    {
        var input = "IR 49 0000 0000 0000 0000 0000 00";

        var ok = IranianIban.TryParse(input, out var normalized);

        ok.ShouldBeTrue();
        normalized.ShouldBe(ValidIbanAllZeros);
    }

    [Fact]
    public void TryParse_ForKnownValidInputInPersianDigits_ReturnsTrue()
    {
        var input = "IR۴۹۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰۰";

        var ok = IranianIban.TryParse(input, out var normalized);

        ok.ShouldBeTrue();
        normalized.ShouldBe(ValidIbanAllZeros);
    }

    [Fact]
    public void TryParse_ForInvalidChecksum_ReturnsFalseButStillNormalizes()
    {
        var ok = IranianIban.TryParse(InvalidChecksumIban, out var normalized);

        ok.ShouldBeFalse();
        normalized.ShouldBe(InvalidChecksumIban);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_WithNullOrEmpty_ReturnsFalseWithEmptyNormalized(string? input)
    {
        var ok = IranianIban.TryParse(input, out var normalized);

        ok.ShouldBeFalse();
        normalized.ShouldBe(string.Empty);
    }
}
