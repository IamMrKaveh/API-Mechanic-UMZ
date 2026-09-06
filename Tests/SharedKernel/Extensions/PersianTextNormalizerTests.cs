using SharedKernel.Extensions;

namespace Tests.SharedKernel.Extensions;

public class PersianTextNormalizerTests
{
    [Theory]
    [InlineData("علي", "علی")]
    [InlineData("كتاب", "کتاب")]
    [InlineData("موسى", "موسی")]
    [InlineData("مدرسة", "مدرسه")]
    [InlineData("مؤمن", "مومن")]
    [InlineData("إسلام", "اسلام")]
    [InlineData("أحمد", "احمد")]
    public void Normalize_ReplacesArabicLettersWithPersian(string input, string expected)
    {
        PersianTextNormalizer.Normalize(input).ShouldBe(expected);
    }

    [Fact]
    public void Normalize_ReplacesArabicIndicDigitsWithPersianDigits()
    {
        PersianTextNormalizer.Normalize("٠١٢٣٤٥٦٧٨٩").ShouldBe("۰۱۲۳۴۵۶۷۸۹");
    }

    [Fact]
    public void Normalize_ReplacesNotSignWithZeroWidthNonJoiner()
    {
        PersianTextNormalizer.Normalize("می¬شود").ShouldBe("می\u200Cشود");
    }

    [Fact]
    public void Normalize_TrimsSurroundingWhitespace()
    {
        PersianTextNormalizer.Normalize("  سلام  ").ShouldBe("سلام");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_BlankInput_ReturnsEmpty(string? input)
    {
        PersianTextNormalizer.Normalize(input!).ShouldBe(string.Empty);
    }

    [Fact]
    public void Normalize_PersianText_StaysUnchanged()
    {
        PersianTextNormalizer.Normalize("سلام دنیا ۱۲۳").ShouldBe("سلام دنیا ۱۲۳");
    }

    [Theory]
    [InlineData("۰۱۲۳۴۵۶۷۸۹", "0123456789")]
    [InlineData("٠١٢٣٤٥٦٧٨٩", "0123456789")]
    [InlineData("abc۱۲۳", "abc123")]
    [InlineData("no digits", "no digits")]
    public void NormalizeDigitsToLatin_ConvertsEasternDigits(string input, string expected)
    {
        PersianTextNormalizer.NormalizeDigitsToLatin(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NormalizeDigitsToLatin_BlankInput_ReturnsEmpty(string? input)
    {
        PersianTextNormalizer.NormalizeDigitsToLatin(input!).ShouldBe(string.Empty);
    }

    [Fact]
    public void RemoveDiacritics_RemovesAllArabicDiacritics()
    {
        PersianTextNormalizer.RemoveDiacritics("بًٌٍَُِّْ").ShouldBe("ب");
    }

    [Fact]
    public void RemoveDiacritics_PlainText_StaysUnchanged()
    {
        PersianTextNormalizer.RemoveDiacritics("سلام").ShouldBe("سلام");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RemoveDiacritics_BlankInput_ReturnsEmpty(string? input)
    {
        PersianTextNormalizer.RemoveDiacritics(input!).ShouldBe(string.Empty);
    }

    [Fact]
    public void CollapseZeroWidthNonJoiner_RemovesZwnjAndNotSign()
    {
        PersianTextNormalizer.CollapseZeroWidthNonJoiner("می\u200Cشود¬تست").ShouldBe("میشودتست");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CollapseZeroWidthNonJoiner_BlankInput_ReturnsEmpty(string? input)
    {
        PersianTextNormalizer.CollapseZeroWidthNonJoiner(input!).ShouldBe(string.Empty);
    }

    [Fact]
    public void Service_DelegatesToStaticNormalizer()
    {
        IPersianTextNormalizer sut = new PersianTextNormalizerService();

        sut.Normalize("علي").ShouldBe("علی");
        sut.NormalizeDigitsToLatin("۱۲۳").ShouldBe("123");
        sut.RemoveDiacritics("بً").ShouldBe("ب");
        sut.CollapseZeroWidthNonJoiner("a\u200Cb").ShouldBe("ab");
    }

    [Fact]
    public void Service_NullInput_ReturnsEmpty()
    {
        IPersianTextNormalizer sut = new PersianTextNormalizerService();

        sut.Normalize(null).ShouldBe(string.Empty);
        sut.NormalizeDigitsToLatin(null).ShouldBe(string.Empty);
        sut.RemoveDiacritics(null).ShouldBe(string.Empty);
        sut.CollapseZeroWidthNonJoiner(null).ShouldBe(string.Empty);
    }

    [Fact]
    public void Service_WhitespaceNormalize_ReturnsEmpty()
    {
        new PersianTextNormalizerService().Normalize("   ").ShouldBe(string.Empty);
    }
}
