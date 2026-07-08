using SharedKernel.Abstractions;
using SharedKernel.Validation;

namespace Domain.Wallet.ValueObjects;

public sealed class IbanNumber : ValueObject
{
    public string Value { get; }

    private IbanNumber(string value) => Value = value;

    public static IbanNumber Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new DomainException("شماره شبا الزامی است.");

        var normalized = IranianIban.Normalize(input);

        if (normalized.Length != IranianIban.TotalLength)
            throw new DomainException("شماره شبا باید دقیقاً ۲۶ کاراکتر باشد.");

        if (!normalized.StartsWith(IranianIban.CountryCode, StringComparison.Ordinal))
            throw new DomainException("شماره شبا باید با IR شروع شود.");

        if (!IranianIban.HasValidFormat(normalized))
            throw new DomainException("شماره شبا فقط باید شامل ارقام باشد.");

        if (!IranianIban.HasValidChecksum(normalized))
            throw new DomainException("شماره شبا نامعتبر است.");

        return new IbanNumber(normalized);
    }

    public static bool TryCreate(string? input, out IbanNumber? iban)
    {
        iban = null;
        if (!IranianIban.TryParse(input, out var normalized)) return false;
        iban = new IbanNumber(normalized);
        return true;
    }

    public string ToMasked()
    {
        if (Value.Length < 10) return Value;
        var start = Value[..6];
        var end = Value[^4..];
        return $"{start}****{end}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}