namespace Domain.Brand.ValueObjects;

public sealed record BrandName
{
    public const int MaxLength = 100;

    public string Value { get; }

    private BrandName(string value) => Value = value;

    public static BrandName Create(BrandName value)
    {
        if (string.IsNullOrWhiteSpace(value.Value))
            throw new ArgumentException("Brand name cannot be empty.", nameof(value));
        if (value.Value.Trim().Length > MaxLength)
            throw new ArgumentException($"Brand name cannot exceed {MaxLength} characters.", nameof(value));
        return new BrandName(value.Value);
    }

    public override string ToString() => Value;
}