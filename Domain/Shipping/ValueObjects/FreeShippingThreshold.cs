namespace Domain.Shipping.ValueObjects;

public sealed class FreeShippingThreshold : ValueObject
{
    public bool IsEnabled { get; }
    public Money? ThresholdAmount { get; }

    private FreeShippingThreshold(bool isEnabled, Money? thresholdAmount)
    {
        IsEnabled = isEnabled;
        ThresholdAmount = thresholdAmount;
    }

    public static FreeShippingThreshold Disabled() => new(false, null);

    public static FreeShippingThreshold Create(Money thresholdAmount)
    {
        Guard.Against.Null(thresholdAmount, nameof(thresholdAmount));

        if (thresholdAmount.IsZero())
            throw new DomainException("آستانه ارسال رایگان باید بزرگتر از صفر باشد.");

        return new FreeShippingThreshold(true, thresholdAmount);
    }

    public bool QualifiesForFreeShipping(Money orderTotal)
    {
        if (!IsEnabled || ThresholdAmount is null)
            return false;

        return orderTotal.IsGreaterThanOrEqual(ThresholdAmount);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return ThresholdAmount?.Amount ?? -1m;
    }
}