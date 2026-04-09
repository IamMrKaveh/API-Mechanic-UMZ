namespace Domain.Shipping.ValueObjects;

public sealed class ShippingOrderRange : ValueObject
{
    public Money? MinOrderAmount { get; }
    public Money? MaxOrderAmount { get; }

    private ShippingOrderRange(Money? minOrderAmount, Money? maxOrderAmount)
    {
        MinOrderAmount = minOrderAmount;
        MaxOrderAmount = maxOrderAmount;
    }

    public static ShippingOrderRange Create(Money? minOrderAmount, Money? maxOrderAmount)
    {
        if (minOrderAmount is not null && maxOrderAmount is not null)
        {
            if (minOrderAmount.IsGreaterThan(maxOrderAmount))
                throw new DomainException("حداقل مبلغ سفارش نمی‌تواند بیشتر از حداکثر باشد.");
        }

        return new ShippingOrderRange(minOrderAmount, maxOrderAmount);
    }

    public static ShippingOrderRange Unlimited() => new(null, null);

    public bool HasMinimum => MinOrderAmount is not null;

    public bool HasMaximum => MaxOrderAmount is not null;

    public bool IsInRange(Money orderTotal)
    {
        if (HasMinimum && MinOrderAmount!.IsGreaterThan(orderTotal))
            return false;

        if (HasMaximum && orderTotal.IsGreaterThan(MaxOrderAmount!))
            return false;

        return true;
    }

    public Result Validate(Money orderTotal)
    {
        if (HasMinimum && MinOrderAmount!.IsGreaterThan(orderTotal))
            return Result.Failure(new Error(
                "400",
                $"حداقل مبلغ سفارش برای این روش ارسال {MinOrderAmount.ToTomanString()} است.",
                ErrorType.Validation));

        if (HasMaximum && orderTotal.IsGreaterThan(MaxOrderAmount!))
            return Result.Failure(new Error(
                "400",
                $"حداکثر مبلغ سفارش برای این روش ارسال {MaxOrderAmount!.ToTomanString()} است.",
                ErrorType.Validation));

        return Result.Success();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MinOrderAmount?.Amount ?? -1m;
        yield return MaxOrderAmount?.Amount ?? -1m;
    }
}