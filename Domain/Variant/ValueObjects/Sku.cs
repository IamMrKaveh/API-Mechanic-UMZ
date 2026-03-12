namespace Domain.Variant.ValueObjects;

public sealed class Sku : ValueObject
{
    public string Value { get; }

    private const int MaxLength = 100;
    private const int MinLength = 1;

    private Sku(string value)
    {
        Value = value;
    }

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("کد SKU الزامی است.");

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length < MinLength)
            throw new DomainException($"کد SKU باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"کد SKU نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z0-9\-_\.]+$"))
            throw new DomainException("کد SKU فقط می‌تواند شامل حروف انگلیسی، اعداد، خط تیره، زیرخط و نقطه باشد.");

        return new Sku(normalized);
    }

    public static Sku FromPersistedString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("کد SKU الزامی است.");
        return new Sku(value);
    }

    public bool Matches(string other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;
        return Value.Equals(other.Trim().ToUpperInvariant(), StringComparison.Ordinal);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(Sku sku) => sku.Value;
}