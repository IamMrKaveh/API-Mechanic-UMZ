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

    public static FreeShippingThreshold Enabled(Money thresholdAmount)
    {
        if (thresholdAmount is null)
            throw new DomainException("FreeShippingThreshold amount is required when enabled.");

        if (thresholdAmount.Amount < 0)
            throw new DomainException("FreeShippingThreshold amount cannot be negative.");

        return new FreeShippingThreshold(true, thresholdAmount);
    }

    public static FreeShippingThreshold Restore(bool isEnabled, Money? thresholdAmount)
    {
        if (isEnabled && thresholdAmount is null)
            throw new DomainException("Enabled FreeShippingThreshold requires a threshold amount.");

        return new FreeShippingThreshold(isEnabled, thresholdAmount);
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