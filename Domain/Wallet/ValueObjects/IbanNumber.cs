using System.Numerics;
using System.Text.RegularExpressions;
using SharedKernel.Abstractions;

namespace Domain.Wallet.ValueObjects;

public sealed class IbanNumber : ValueObject
{
    private const int IranIbanLength = 26;
    private const string CountryCode = "IR";
    private static readonly Regex DigitsOnly = new("^[0-9]+$", RegexOptions.Compiled);

    public string Value { get; }

    private IbanNumber(string value) => Value = value;

    public static IbanNumber Create(string input)
    {
        var normalized = Normalize(input);

        if (normalized.Length != IranIbanLength)
            throw new DomainException("شماره شبا باید دقیقاً ۲۶ کاراکتر باشد.");

        if (!normalized.StartsWith(CountryCode, StringComparison.Ordinal))
            throw new DomainException("شماره شبا باید با IR شروع شود.");

        var digits = normalized[2..];
        if (!DigitsOnly.IsMatch(digits))
            throw new DomainException("شماره شبا فقط باید شامل ارقام باشد.");

        if (!IsMod97Valid(normalized))
            throw new DomainException("شماره شبا نامعتبر است.");

        return new IbanNumber(normalized);
    }

    public string ToMasked()
    {
        if (Value.Length < 10) return Value;
        var start = Value[..6];
        var end = Value[^4..];
        return $"{start}****{end}";
    }

    private static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new DomainException("شماره شبا الزامی است.");

        var sb = new System.Text.StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsWhiteSpace(ch) || ch == '-') continue;
            sb.Append(char.ToUpperInvariant(ch));
        }

        var value = sb.ToString();

        if (!value.StartsWith(CountryCode, StringComparison.Ordinal) &&
            value.Length == IranIbanLength - 2 &&
            DigitsOnly.IsMatch(value))
        {
            value = CountryCode + value;
        }

        return value;
    }

    private static bool IsMod97Valid(string iban)
    {
        var rearranged = iban[4..] + iban[..4];
        var numeric = new System.Text.StringBuilder(rearranged.Length * 2);

        foreach (var ch in rearranged)
        {
            if (char.IsDigit(ch))
            {
                numeric.Append(ch);
            }
            else if (ch is >= 'A' and <= 'Z')
            {
                numeric.Append(ch - 'A' + 10);
            }
            else
            {
                return false;
            }
        }

        if (!BigInteger.TryParse(numeric.ToString(), out var value))
            return false;

        return value % 97 == 1;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}