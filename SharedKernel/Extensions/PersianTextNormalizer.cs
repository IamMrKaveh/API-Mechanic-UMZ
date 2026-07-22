namespace SharedKernel.Extensions;

public static class PersianTextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("ى", "ی")
            .Replace("ة", "ه")
            .Replace("ؤ", "و")
            .Replace("إ", "ا")
            .Replace("أ", "ا")
            .Replace("٠", "۰")
            .Replace("١", "۱")
            .Replace("٢", "۲")
            .Replace("٣", "۳")
            .Replace("٤", "۴")
            .Replace("٥", "۵")
            .Replace("٦", "۶")
            .Replace("٧", "۷")
            .Replace("٨", "۸")
            .Replace("٩", "۹")
            .Replace("¬", "‌");
    }

    public static string NormalizeDigitsToLatin(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var buffer = new char[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch is >= '\u06F0' and <= '\u06F9')
                buffer[i] = (char)('0' + (ch - '\u06F0'));
            else if (ch is >= '\u0660' and <= '\u0669')
                buffer[i] = (char)('0' + (ch - '\u0660'));
            else
                buffer[i] = ch;
        }
        return new string(buffer);
    }

    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("ً", string.Empty)
            .Replace("ٌ", string.Empty)
            .Replace("ٍ", string.Empty)
            .Replace("َ", string.Empty)
            .Replace("ُ", string.Empty)
            .Replace("ِ", string.Empty)
            .Replace("ّ", string.Empty)
            .Replace("ْ", string.Empty);
    }

    public static string CollapseZeroWidthNonJoiner(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Replace("\u200C", string.Empty).Replace("¬", string.Empty);
    }
}

public sealed class PersianTextNormalizerService : IPersianTextNormalizer
{
    public string Normalize(string? text)
        => string.IsNullOrWhiteSpace(text) ? string.Empty : PersianTextNormalizer.Normalize(text);

    public string NormalizeDigitsToLatin(string? text)
        => text is null ? string.Empty : PersianTextNormalizer.NormalizeDigitsToLatin(text);

    public string RemoveDiacritics(string? text)
        => text is null ? string.Empty : PersianTextNormalizer.RemoveDiacritics(text);

    public string CollapseZeroWidthNonJoiner(string? text)
        => text is null ? string.Empty : PersianTextNormalizer.CollapseZeroWidthNonJoiner(text);
}
