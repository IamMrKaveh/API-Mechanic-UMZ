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

        var calculated = percentage.ApplyTo(originalPrice);
        var safeValue = Math.Min(calculated, originalPrice);

        return new DiscountAmount(safeValue);
    }

    public static DiscountAmount Fixed(decimal originalPrice, decimal fixedDiscount)
    {
        if (originalPrice < 0)
            throw new DomainException("Original price cannot be negative.");

        if (fixedDiscount < 0)
            throw new DomainException("Fixed discount cannot be negative.");

        var safeValue = Math.Min(fixedDiscount, originalPrice);

        return new DiscountAmount(safeValue);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}