namespace Domain.Variant.ValueObjects;

public sealed class Sku : ValueObject
{
    public string Value { get; }

    private Sku(string value)
    {
        Value = value;
    }

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("SKU الزامی است.");

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > 50)
            throw new DomainException("SKU نباید بیشتر از ۵۰ کاراکتر باشد.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[A-Z0-9\-_]+$"))
            throw new DomainException("SKU فقط می‌تواند شامل حروف، اعداد و خط تیره باشد.");

        return new Sku(normalized);
    }

    public static Sku? CreateOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Create(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Sku sku) => sku.Value;
}