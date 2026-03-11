namespace Domain.Discount.ValueObjects;

public sealed class DiscountCodeValue : ValueObject
{
    private const int MaxLength = 50;

    public string Value { get; }

    private DiscountCodeValue(string value) => Value = value;

    public static DiscountCodeValue Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Discount code cannot be empty.", nameof(value));

        if (value.Length > MaxLength)
            throw new ArgumentException($"Discount code cannot exceed {MaxLength} characters.", nameof(value));

        return new DiscountCodeValue(value.ToUpperInvariant().Trim());
    }

    public static DiscountCodeValue FromPersistedString(string value) => new(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(DiscountCodeValue codeValue) => codeValue.Value;
}