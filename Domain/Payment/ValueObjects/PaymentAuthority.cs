namespace Domain.Payment.ValueObjects;

public sealed class PaymentAuthority : ValueObject
{
    public string Value { get; }

    private const int MaxLength = 100;
    private const int MinLength = 10;

    private PaymentAuthority(string value)
    {
        Value = value;
    }

    public static PaymentAuthority Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("شناسه پرداخت الزامی است.");

        var normalized = value.Trim();

        if (normalized.Length < MinLength)
            throw new DomainException($"شناسه پرداخت باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"شناسه پرداخت نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return new PaymentAuthority(normalized);
    }

    public static (bool Success, PaymentAuthority? Authority, string? Error) TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (false, null, "شناسه پرداخت الزامی است.");

        var normalized = value.Trim();

        if (normalized.Length < MinLength)
            return (false, null, $"شناسه پرداخت باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            return (false, null, $"شناسه پرداخت نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return (true, new PaymentAuthority(normalized), null);
    }

    public bool Matches(string other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        return Value.Equals(other.Trim(), StringComparison.Ordinal);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PaymentAuthority authority) => authority.Value;
}