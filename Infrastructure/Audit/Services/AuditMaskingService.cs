using RegexOptions = System.Text.RegularExpressions.RegexOptions;

namespace Infrastructure.Audit.Services;

/// <summary>
/// سرویس Masking اطلاعات حساس در لاگ‌های حسابرسی.
///
/// اطلاعاتی که Mask می‌شوند:
/// - شماره کارت بانکی
/// - شماره موبایل
/// - رمز عبور
/// - توکن‌های احراز هویت
/// - ایمیل (اختیاری)
/// - کد ملی
/// </summary>

public sealed class AuditMaskingService : IAuditMaskingService
{
    // ─── الگوهای Regex ───────────────────────────────────────────────────────

    // شماره کارت: 16 رقم با/بدون خط تیره
    private static readonly Regex CardNumberRegex = new(
        @"\b(\d{4})[\s\-]?(\d{4})[\s\-]?(\d{4})[\s\-]?(\d{4})\b",
        RegexOptions.Compiled);

    // شماره موبایل ایرانی
    private static readonly Regex PhoneRegex = new(
        @"\b(0?9[0-9]{2})[\s\-]?(\d{3})[\s\-]?(\d{4})\b",
        RegexOptions.Compiled);

    // ایمیل
    private static readonly Regex EmailRegex = new(
        @"\b([a-zA-Z0-9._%+\-]+)@([a-zA-Z0-9.\-]+\.[a-zA-Z]{2,})\b",
        RegexOptions.Compiled);

    // کد ملی ایرانی (10 رقم)
    private static readonly Regex NationalCodeRegex = new(
        @"\b(\d{3})(\d{4})(\d{3})\b",
        RegexOptions.Compiled);

    // Bearer Token و JWT
    private static readonly Regex BearerTokenRegex = new(
        @"(Bearer\s+)[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // رمز عبور در JSON
    private static readonly Regex PasswordFieldRegex = new(
        @"""(password|passwd|secret|token|apikey|api_key)""\s*:\s*""([^""]*)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // IBAN / شبا
    private static readonly Regex IbanRegex = new(
        @"\bIR\d{24}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ─── Implementation ──────────────────────────────────────────────────────

    public string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = input;

        // ترتیب مهم است - ابتدا الگوهای دقیق‌تر اعمال می‌شوند

        // 1. Bearer Token
        result = BearerTokenRegex.Replace(result, "$1[MASKED-TOKEN]");

        // 2. رمز عبور در JSON
        result = PasswordFieldRegex.Replace(result, "\"$1\":\"[MASKED]\"");

        // 3. شماره کارت
        result = CardNumberRegex.Replace(result, m =>
            $"{m.Groups[1].Value}-****-****-{m.Groups[4].Value}");

        // 4. IBAN
        result = IbanRegex.Replace(result, ir =>
            $"IR{ir.Value[2..6]}****{ir.Value[^4..]}");

        // 5. شماره موبایل
        result = PhoneRegex.Replace(result, m =>
            $"{m.Groups[1].Value}-***-{m.Groups[3].Value}");

        // 6. ایمیل - فقط بخش username را Mask می‌کند
        result = EmailRegex.Replace(result, m =>
            $"{MaskEmail(m.Groups[1].Value)}@{m.Groups[2].Value}");

        // 7. کد ملی (محتاطانه - چون ممکن است با اعداد دیگر تداخل داشته باشد)
        // فقط در context مشخص Mask می‌شود
        result = MaskNationalCodeInContext(result);

        return result;
    }

    public string MaskDetails(string details)
    {
        return MaskSensitiveData(details);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private static string MaskEmail(string username)
    {
        if (username.Length <= 2) return "**";
        return username[0] + new string('*', Math.Min(username.Length - 2, 5)) + username[^1];
    }

    private static string MaskNationalCodeInContext(string input)
    {
        // فقط زمانی که با کلمه کلیدی مشخص همراه باشد
        var contextPattern = new Regex(
            @"(ملی|national.?code|nationalcode|national_id)\s*[=:]\s*(\d{10})",
            RegexOptions.IgnoreCase);

        return contextPattern.Replace(input, m =>
            $"{m.Groups[1].Value}:***{m.Groups[2].Value[7..]}");
    }
}

// ─── Masking Extensions ───────────────────────────────────────────────────────

public static class MaskingExtensions
{
    /// <summary>Mask کردن شماره کارت برای نمایش (نگه داشتن 4 رقم آخر)</summary>
    public static string MaskCardPan(this string? cardPan)
    {
        if (string.IsNullOrEmpty(cardPan)) return "****";
        var cleaned = cardPan.Replace("-", "").Replace(" ", "");
        if (cleaned.Length < 4) return "****";
        return $"****-****-****-{cleaned[^4..]}";
    }

    /// <summary>Mask کردن شماره موبایل (نگه داشتن 4 رقم آخر)</summary>
    public static string MaskPhone(this string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return "***";
        if (phone.Length < 4) return "***";
        return $"0***{phone[^4..]}";
    }
}