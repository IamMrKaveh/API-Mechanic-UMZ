namespace Domain.Discount.ValueObjects;

public sealed class DiscountAmount : ValueObject
{
    public decimal Value { get; }

    private DiscountAmount(decimal value)
    {
        Value = value;
    }

    public static DiscountAmount Calculate(decimal originalPrice, Percentage percentage)
    {
        if (originalPrice < 0)
            throw new DomainException("Original price cannot be negative.");

        return new DiscountAmount(percentage.Apply(originalPrice));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}