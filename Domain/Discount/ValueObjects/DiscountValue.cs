using Domain.Discount.Enums;

namespace Domain.Discount.ValueObjects;

public sealed class DiscountValue : ValueObject
{
    public decimal Amount { get; }
    public DiscountType Type { get; }

    private DiscountValue(decimal amount, DiscountType type)
    {
        Amount = amount;
        Type = type;
    }

    public static DiscountValue Percentage(decimal percent)
    {
        if (percent is <= 0 or > 100)
            throw new DomainException($"درصد تخفیف باید بین ۰ و ۱۰۰ باشد. مقدار: {percent}");

        return new DiscountValue(percent, DiscountType.Percentage);
    }

    public static DiscountValue Fixed(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException($"مبلغ تخفیف ثابت باید بیشتر از صفر باشد. مقدار: {amount}");

        return new DiscountValue(amount, DiscountType.FixedAmount);
    }

    public static DiscountValue FreeShipping() => new DiscountValue(0, DiscountType.FreeShipping);

    public Money Apply(Money originalPrice)
    {
        return Type switch
        {
            DiscountType.Percentage => originalPrice.Multiply(1 - Amount / 100m),
            DiscountType.FixedAmount =>
                originalPrice.Amount > Amount
                    ? originalPrice.Subtract(Money.FromDecimal(Amount, originalPrice.Currency))
                    : Money.Zero(originalPrice.Currency),
            DiscountType.FreeShipping => originalPrice,
            _ => throw new DomainException("نوع تخفیف نامعتبر است.")
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Type;
    }
}