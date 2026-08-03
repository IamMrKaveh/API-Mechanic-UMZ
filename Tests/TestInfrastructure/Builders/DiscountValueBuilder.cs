using Domain.Discount.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class DiscountValueBuilder
{
    private DiscountValue _value = DiscountValue.Percentage(10m);

    public DiscountValueBuilder AsPercentage(decimal percent)
    {
        _value = DiscountValue.Percentage(percent);
        return this;
    }

    public DiscountValueBuilder AsFixed(decimal amount)
    {
        _value = DiscountValue.Fixed(amount);
        return this;
    }

    public DiscountValueBuilder AsFreeShipping()
    {
        _value = DiscountValue.FreeShipping();
        return this;
    }

    public DiscountValue Build() => _value;
}
