namespace Domain.Shipping.Rules;

public sealed class ShippingOrderAmountInRangeRule(Aggregates.Shipping shipping, Money orderTotal) : IBusinessRule
{
    private readonly Aggregates.Shipping _shipping = shipping;
    private readonly Money _orderTotal = orderTotal;

    public bool IsBroken()
    {
        return !_shipping.IsAvailableForOrder(_orderTotal);
    }

    public string Message
    {
        get
        {
            if (!_shipping.IsActive)
                return "روش ارسال غیرفعال است.";

            var range = _shipping.OrderRange;

            if (range.HasMinimum && range.MinOrderAmount!.IsGreaterThan(_orderTotal))
                return $"حداقل مبلغ سفارش برای این روش ارسال {range.MinOrderAmount.ToTomanString()} است.";

            if (range.HasMaximum && _orderTotal.IsGreaterThan(range.MaxOrderAmount!))
                return $"حداکثر مبلغ سفارش برای این روش ارسال {range.MaxOrderAmount!.ToTomanString()} است.";

            return "مبلغ سفارش در محدوده مجاز این روش ارسال نیست.";
        }
    }
}