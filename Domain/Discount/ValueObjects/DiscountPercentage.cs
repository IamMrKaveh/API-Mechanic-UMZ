namespace Domain.Discount.ValueObjects;

public sealed class DiscountPercentage : ValueObject
{
    public decimal Value { get; }

    private const decimal MinValue = 0.01m;
    private const decimal MaxValue = 100m;

    private DiscountPercentage(decimal value)
    {
        Value = value;
    }

    public static DiscountPercentage Create(decimal value)
    {
        if (value < MinValue)
            throw new DomainException("درصد تخفیف باید بزرگتر از صفر باشد.");

        if (value > MaxValue)
            throw new DomainException("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");

        return new DiscountPercentage(value);
    }

    public static DiscountPercentage Zero => new(0);
    public static DiscountPercentage Full => new(100);

    public decimal CalculateDiscount(decimal amount)
    {
        return Math.Round(amount * Value / 100, 0);
    }

    public Money CalculateDiscountMoney(Money amount)
    {
        return Money.FromDecimal(CalculateDiscount(amount.Amount));
    }

    public bool IsZero => Value == 0;
    public bool IsFull => Value == 100;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value}%";

    public static implicit operator decimal(DiscountPercentage percentage) => percentage.Value;
}