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
}