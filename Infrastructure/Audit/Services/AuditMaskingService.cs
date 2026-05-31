using Domain.User.ValueObjects;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;

namespace Infrastructure.Audit.Services;

public sealed partial class AuditMaskingService : IAuditMaskingService
{
    private static readonly Regex CardNumberRegex = cardNumberRegex();

    private static readonly Regex PhoneRegex = phoneRegex();

    private static readonly Regex EmailRegex = emailRegex();

    private static readonly Regex BearerTokenRegex = bearerTokenRegex();

    private static readonly Regex IpV4Regex = ipV4Regex();

    public string MaskPhoneNumber(PhoneNumber phoneNumber)
    {
        if (phoneNumber is null) return string.Empty;
        var value = phoneNumber.Value;
        if (value.Length < 7) return new string('*', value.Length);
        return $"{value[..3]}****{value[^4..]}";
    }

    public string MaskEmail(Email email)
    {
        if (email is null) return string.Empty;
        var value = email.Value;
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0) return "***@***";
        var username = value[..atIndex];
        var domain = value[atIndex..];
        if (username.Length <= 2) return $"**{domain}";
        return $"{username[0]}{new string('*', Math.Min(username.Length - 2, 5))}{username[^1]}{domain}";
    }

    public string MaskIpAddress(IpAddress ipAddress)
    {
        if (ipAddress is null) return string.Empty;
        var value = ipAddress.Value;
        var parts = value.Split('.');
        if (parts.Length == 4)
            return $"{parts[0]}.{parts[1]}.*.*";
        return value.Length > 4 ? $"{value[..4]}****" : "***";
    }

    public string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = input;
        result = BearerTokenRegex.Replace(result, "$1[MASKED-TOKEN]");
        result = CardNumberRegex.Replace(result, m => $"{m.Groups[1].Value}-****-****-{m.Groups[4].Value}");
        result = PhoneRegex.Replace(result, m => $"{m.Groups[1].Value}-***-{m.Groups[3].Value}");
        result = EmailRegex.Replace(result, m => $"{MaskEmailUsername(m.Groups[1].Value)}@{m.Groups[2].Value}");

        return result;
    }

    private static string MaskEmailUsername(string username)
    {
        if (username.Length <= 2) return "**";
        return $"{username[0]}{new string('*', Math.Min(username.Length - 2, 5))}{username[^1]}";
    }

    [GeneratedRegex(@"\b(\d{4})[\s\-]?(\d{4})[\s\-]?(\d{4})[\s\-]?(\d{4})\b", RegexOptions.Compiled)]
    private static partial Regex cardNumberRegex();

    [GeneratedRegex(@"\b(0?9[0-9]{2})[\s\-]?(\d{3})[\s\-]?(\d{4})\b", RegexOptions.Compiled)]
    private static partial Regex phoneRegex();

    [GeneratedRegex(@"\b([a-zA-Z0-9._%+\-]+)@([a-zA-Z0-9.\-]+\.[a-zA-Z]{2,})\b", RegexOptions.Compiled)]
    private static partial Regex emailRegex();

    [GeneratedRegex(@"(Bearer\s+)[A-Za-z0-9\-._~+/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex bearerTokenRegex();

    [GeneratedRegex(@"\b(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\b", RegexOptions.Compiled)]
    private static partial Regex ipV4Regex();
}