namespace Domain.Brand.ValueObjects;

public sealed record BrandName
{
    public const int MaxLength = 100;

    public string Value { get; }

    private BrandName(string value) => Value = value;

    public static BrandName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Brand name cannot be empty.", nameof(value));
        if (value.Trim().Length > MaxLength)
            throw new ArgumentException($"Brand name cannot exceed {MaxLength} characters.", nameof(value));
        return new BrandName(value.Trim());
    }

    public override string ToString() => Value;
}