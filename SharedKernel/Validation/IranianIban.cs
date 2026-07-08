using System.Numerics;
using System.Text;

namespace SharedKernel.Validation;

public static class IranianIban
{
    public const int TotalLength = 26;
    public const int BodyDigitsLength = 24;
    public const string CountryCode = "IR";

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') continue;

            var mapped = MapDigit(ch);
            if (mapped != '\0')
            {
                sb.Append(mapped);
                continue;
            }

            sb.Append(char.ToUpperInvariant(ch));
        }

        var value = sb.ToString();

        if (value.Length == BodyDigitsLength && IsAllAsciiDigits(value))
            value = CountryCode + value;

        return value;
    }

    public static bool HasValidFormat(string normalized)
    {
        if (string.IsNullOrEmpty(normalized)) return false;
        if (normalized.Length != TotalLength) return false;
        if (!normalized.StartsWith(CountryCode, StringComparison.Ordinal)) return false;
        return IsAllAsciiDigits(normalized.AsSpan(2));
    }

    public static bool HasValidChecksum(string normalized)
    {
        if (!HasValidFormat(normalized)) return false;

        var rearranged = string.Concat(normalized.AsSpan(4), normalized.AsSpan(0, 4));
        var numeric = new StringBuilder(rearranged.Length * 2);

        foreach (var ch in rearranged)
        {
            if (ch is >= '0' and <= '9')
                numeric.Append(ch);
            else if (ch is >= 'A' and <= 'Z')
                numeric.Append(ch - 'A' + 10);
            else
                return false;
        }

        return BigInteger.TryParse(numeric.ToString(), out var value) && value % 97 == 1;
    }

    public static bool TryParse(string? input, out string normalized)
    {
        normalized = Normalize(input);
        return HasValidChecksum(normalized);
    }

    private static bool IsAllAsciiDigits(ReadOnlySpan<char> value)
    {
        foreach (var ch in value)
            if (ch is < '0' or > '9') return false;
        return value.Length > 0;
    }

    private static bool IsAllAsciiDigits(string value) => IsAllAsciiDigits(value.AsSpan());

    private static char MapDigit(char ch)
    {
        if (ch is >= '\u06F0' and <= '\u06F9') return (char)('0' + (ch - '\u06F0'));
        if (ch is >= '\u0660' and <= '\u0669') return (char)('0' + (ch - '\u0660'));
        return '\0';
    }
}