namespace Domain.Shipping.Rules;

public sealed class ShippingMustBeActiveForOrderRule(Aggregates.Shipping shipping) : IBusinessRule
{
    private readonly Aggregates.Shipping _shipping = shipping;

    public bool IsBroken()
    {
        return !_shipping.IsActive;
    }

    public string Message => "روش ارسال انتخاب شده غیرفعال است و قابل استفاده در سفارش نیست.";
}