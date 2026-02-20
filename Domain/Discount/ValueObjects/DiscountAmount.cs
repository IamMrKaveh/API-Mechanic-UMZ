using Domain.Common.Shared.ValueObjects;

namespace Domain.Discount.ValueObjects;

public sealed class DiscountAmount : ValueObject
{
    public Money Value { get; }
    public decimal Percentage { get; }
    public bool IsPercentage { get; }

    private DiscountAmount(Money value, decimal percentage, bool isPercentage)
    {
        Value = value;
        Percentage = percentage;
        IsPercentage = isPercentage;
    }

    public static DiscountAmount FromPercentage(decimal percentage)
    {
        if (percentage <= 0)
            throw new DomainException("درصد تخفیف باید بزرگتر از صفر باشد.");

        if (percentage > 100)
            throw new DomainException("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");

        return new DiscountAmount(Money.Zero(), percentage, true);
    }

    public static DiscountAmount FromFixedAmount(Money amount)
    {
        if (amount.Amount <= 0)
            throw new DomainException("مبلغ تخفیف باید بزرگتر از صفر باشد.");

        return new DiscountAmount(amount, 0, false);
    }

    public Money CalculateDiscount(Money orderTotal, Money? maxDiscount = null)
    {
        Money discount;

        if (IsPercentage)
        {
            discount = Money.FromDecimal(Math.Round(orderTotal.Amount * Percentage / 100, 0));
        }
        else
        {
            discount = Value;
        }

        // محدود کردن به مبلغ کل سفارش
        if (discount.Amount > orderTotal.Amount)
        {
            discount = orderTotal;
        }

        // اعمال سقف تخفیف
        if (maxDiscount != null && discount.Amount > maxDiscount.Amount)
        {
            discount = maxDiscount;
        }

        return discount;
    }

    public string GetDisplayText()
    {
        if (IsPercentage)
            return $"{Percentage}%";

        return Value.ToTomanString();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsPercentage;
        yield return IsPercentage ? Percentage : Value.Amount;
    }

    public override string ToString() => GetDisplayText();
}