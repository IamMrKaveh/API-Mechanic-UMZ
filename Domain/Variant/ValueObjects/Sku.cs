namespace Domain.Variant.ValueObjects;

public sealed partial class Sku : ValueObject
{
    public string Value { get; private set; } = string.Empty;

    private const int MaxLength = 100;
    private const int MinLength = 1;

    private Sku(string value) => Value = value;

    private Sku()
    { }

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("کد SKU الزامی است.");

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length < MinLength)
            throw new DomainException($"کد SKU باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"کد SKU نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        if (!VariantSkuRegex().IsMatch(normalized))
            throw new DomainException("کد SKU فقط می‌تواند شامل حروف انگلیسی، اعداد، خط تیره، زیرخط و نقطه باشد.");

        return new Sku(normalized);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    public static implicit operator string(Sku sku) => sku.Value;

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Z0-9\-_\.]+$")]
    private static partial System.Text.RegularExpressions.Regex VariantSkuRegex();
}
