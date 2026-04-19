namespace Domain.Payment.ValueObjects;

public sealed class PaymentAuthority : ValueObject
{
    private const int MinLength = 5;
    private const int MaxLength = 200;

    public string Value { get; }

    private PaymentAuthority(string value) => Value = value;

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

    public bool Matches(string other) => !string.IsNullOrWhiteSpace(other) && Value.Equals(other.Trim(), StringComparison.Ordinal);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PaymentAuthority authority) => authority.Value;
}