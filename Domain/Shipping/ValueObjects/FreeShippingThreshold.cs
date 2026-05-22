namespace Domain.Shipping.ValueObjects;

public sealed class FreeShippingThreshold : ValueObject
{
    public bool IsEnabled { get; }
    public Money? ThresholdAmount { get; }

    public FreeShippingThreshold()
    {
    }

    private FreeShippingThreshold(bool isEnabled, Money? thresholdAmount)
    {
        IsEnabled = isEnabled;
        ThresholdAmount = thresholdAmount;
    }

    public static FreeShippingThreshold Disabled() => new(false, null);

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