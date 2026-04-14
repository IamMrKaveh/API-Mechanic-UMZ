using Application.Audit.Contracts;
using Domain.User.ValueObjects;
using Domain.Security.ValueObjects;
using System.Text.RegularExpressions;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;

namespace Infrastructure.Audit.Services;

public sealed class AuditMaskingService : IAuditMaskingService
{
    private static readonly Regex CardNumberRegex = new(
        @"\b(\d{4})[\s\-]?(\d{4})[\s\-]?(\d{4})[\s\-]?(\d{4})\b",
        RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"\b(0?9[0-9]{2})[\s\-]?(\d{3})[\s\-]?(\d{4})\b",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"\b([a-zA-Z0-9._%+\-]+)@([a-zA-Z0-9.\-]+\.[a-zA-Z]{2,})\b",
        RegexOptions.Compiled);

    private static readonly Regex BearerTokenRegex = new(
        @"(Bearer\s+)[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex IpV4Regex = new(
        @"\b(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\b",
        RegexOptions.Compiled);

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
}