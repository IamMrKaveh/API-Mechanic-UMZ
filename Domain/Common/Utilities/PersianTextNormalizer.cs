namespace Domain.Common.Utilities;

public static class PersianTextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("ى", "ی");
    }
}