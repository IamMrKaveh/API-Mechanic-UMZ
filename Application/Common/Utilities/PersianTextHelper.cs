namespace Application.Common.Utilities;

public static class PersianTextHelper
{
    public static string Normalize(
        string? text
        )
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        return text.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("ى", "ی")
            .Replace("٤", "۴")
            .Replace("٥", "۵")
            .Replace("٦", "۶")
            .Replace("¬", "‌");
    }
}