namespace Domain.Order.Rules;

public sealed class ShippedOrderCannotBeCancelledRule : IBusinessRule
{
    private readonly Order _order;

    public ShippedOrderCannotBeCancelledRule(Order order)
    {
        _order = order;
    }

    public bool IsBroken()
    {
        return _order.IsShipped || _order.IsDelivered;
    }

    public string Message => _order.IsDelivered
        ? "سفارش تحویل داده شده قابل لغو نیست."
        : "سفارش ارسال شده قابل لغو نیست.";
}