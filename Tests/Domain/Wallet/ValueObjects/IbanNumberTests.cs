using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Wallet.ValueObjects;

public class IbanNumberTests
{
    // Real Iranian IBANs whose MOD-97 checksum has been pre-computed against
    // SharedKernel.Validation.IranianIban.HasValidChecksum.
    private const string ValidIban = "IR200170000000000123456789";
    private const string ValidIban2 = "IR550620000000001234567890";
    private const string ValidIban3 = "IR460540000000009876543210";

    // Same digits as ValidIban but with the last digit mutated to break the checksum.
    private const string ValidIbanWithBrokenChecksum = "IR200170000000000123456780";

    // ValidIban's 24 body digits (no "IR" prefix). Normalize must auto-prepend "IR".
    private const string ValidIbanBodyOnly = "200170000000000123456789";

    // ValidIban's digit portion re-encoded with Persian digits.
    private const string ValidIbanWithPersianDigits =
        "IR\u06F2\u06F0\u06F0\u06F1\u06F7\u06F0\u06F0\u06F0\u06F0\u06F0\u06F0\u06F0\u06F0\u06F0\u06F0\u06F1\u06F2\u06F3\u06F4\u06F5\u06F6\u06F7\u06F8\u06F9";

    #region Create - success

    [Fact]
    public void Create_WithValidIban_ReturnsInstanceWithNormalizedValue()
    {
        var sut = IbanNumber.Create(ValidIban);

        sut.Value.ShouldBe(ValidIban);
    }

    [Theory]
    [InlineData(ValidIban)]
    [InlineData(ValidIban2)]
    [InlineData(ValidIban3)]
    public void Create_WithMultipleValidIbans_ReturnsInstanceForEach(string input)
    {
        var sut = IbanNumber.Create(input);

        sut.Value.ShouldBe(input);
    }

    [Fact]
    public void Create_WithSpacedFormatting_NormalizesAndReturnsIban()
    {
        var sut = IbanNumber.Create("IR20 0170 0000 0000 0123 4567 89");

        sut.Value.ShouldBe(ValidIban);
    }

    [Fact]
    public void Create_WithDashedFormatting_NormalizesAndReturnsIban()
    {
        var sut = IbanNumber.Create("IR20-0170-0000-0000-0123-4567-89");

        sut.Value.ShouldBe(ValidIban);
    }

    [Fact]
    public void Create_WithUnderscoreFormatting_NormalizesAndReturnsIban()
    {
        var sut = IbanNumber.Create("IR20_0170_0000_0000_0123_4567_89");

        sut.Value.ShouldBe(ValidIban);
    }

    [Fact]
    public void Create_WithLowercaseCountryCode_UppercasesAndReturnsIban()
    {
        var sut = IbanNumber.Create("ir200170000000000123456789");

        sut.Value.ShouldBe(ValidIban);
    }

    [Fact]
    public void Create_WithBodyOnly24Digits_PrependsCountryCodeAndReturnsIban()
    {
        var sut = IbanNumber.Create(ValidIbanBodyOnly);

        sut.Value.ShouldBe(ValidIban);
    }

    [Fact]
    public void Create_WithPersianDigits_ConvertsToAsciiAndReturnsIban()
    {
        var sut = IbanNumber.Create(ValidIbanWithPersianDigits);

        sut.Value.ShouldBe(ValidIban);
    }

    #endregion

    #region Create - failure paths

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void Create_WithNullOrWhitespace_ThrowsDomainExceptionWithRequiredMessage(string? input)
    {
        var exception = Should.Throw<DomainException>(() => IbanNumber.Create(input!));

        exception.Message.ShouldBe("شماره شبا الزامی است.");
    }

    [Theory]
    [InlineData("IR2001700000")]                        // too short after normalization
    [InlineData("IR20017000000000012345678901234567")]  // too long after normalization
    public void Create_WithInvalidLength_ThrowsDomainExceptionWithLengthMessage(string input)
    {
        var exception = Should.Throw<DomainException>(() => IbanNumber.Create(input));

        exception.Message.ShouldBe("شماره شبا باید دقیقاً ۲۶ کاراکتر باشد.");
    }

    [Fact]
    public void Create_WithWrongCountryCodeButCorrectLength_ThrowsDomainExceptionWithCountryMessage()
    {
        // 26 chars total, starts with "US" (not "IR") after uppercasing.
        var exception = Should.Throw<DomainException>(
            () => IbanNumber.Create("US200170000000000123456789"));

        exception.Message.ShouldBe("شماره شبا باید با IR شروع شود.");
    }

    [Fact]
    public void Create_WithNonDigitCharactersAfterCountryCode_ThrowsDomainExceptionWithFormatMessage()
    {
        // 26 chars, starts with "IR", but contains letters after the country code.
        var exception = Should.Throw<DomainException>(
            () => IbanNumber.Create("IRAB0170000000000123456789"));

        exception.Message.ShouldBe("شماره شبا فقط باید شامل ارقام باشد.");
    }

    [Fact]
    public void Create_WithInvalidChecksum_ThrowsDomainExceptionWithInvalidMessage()
    {
        var exception = Should.Throw<DomainException>(
            () => IbanNumber.Create(ValidIbanWithBrokenChecksum));

        exception.Message.ShouldBe("شماره شبا نامعتبر است.");
    }

    #endregion

    #region TryCreate

    [Fact]
    public void TryCreate_WithValidIban_ReturnsTrueAndOutputsInstance()
    {
        var success = IbanNumber.TryCreate(ValidIban, out var iban);

        success.ShouldBeTrue();
        iban.ShouldNotBeNull();
        iban!.Value.ShouldBe(ValidIban);
    }

    [Fact]
    public void TryCreate_WithFormattedValidIban_ReturnsTrueAndOutputsNormalizedInstance()
    {
        var success = IbanNumber.TryCreate("IR20 0170 0000 0000 0123 4567 89", out var iban);

        success.ShouldBeTrue();
        iban.ShouldNotBeNull();
        iban!.Value.ShouldBe(ValidIban);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-iban")]
    [InlineData("IR2001700000")]                        // too short
    [InlineData("US200170000000000123456789")]           // wrong country
    [InlineData("IRAB0170000000000123456789")]           // non-digit body
    [InlineData(ValidIbanWithBrokenChecksum)]            // bad checksum
    public void TryCreate_WithInvalidInput_ReturnsFalseAndNullsOutput(string? input)
    {
        var success = IbanNumber.TryCreate(input, out var iban);

        success.ShouldBeFalse();
        iban.ShouldBeNull();
    }

    #endregion

    #region ToMasked

    [Fact]
    public void ToMasked_ForValidIban_ShowsFirstSixAndLastFourWithStarsInBetween()
    {
        var sut = IbanNumber.Create(ValidIban);

        sut.ToMasked().ShouldBe("IR2001****6789");
    }

    [Fact]
    public void ToMasked_ForValidIban_ProducesFixedFourteenCharacterOutput()
    {
        var sut = IbanNumber.Create(ValidIban);

        sut.ToMasked().Length.ShouldBe(14);
    }

    [Fact]
    public void ToMasked_ForAnotherValidIban_ShowsCorrectPrefixAndSuffix()
    {
        var sut = IbanNumber.Create(ValidIban2);

        sut.ToMasked().ShouldBe("IR5506****7890");
    }

    #endregion

    #region Equality (ValueObject semantics)

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var a = IbanNumber.Create(ValidIban);
        var b = IbanNumber.Create(ValidIban);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equals_WithSameLogicalValueButDifferentFormatting_ReturnsTrue()
    {
        var a = IbanNumber.Create(ValidIban);
        var b = IbanNumber.Create("IR20 0170 0000 0000 0123 4567 89");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        var a = IbanNumber.Create(ValidIban);
        var b = IbanNumber.Create(ValidIban2);

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }

    #endregion

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        var sut = IbanNumber.Create(ValidIban);

        sut.ToString().ShouldBe(ValidIban);
    }
}

