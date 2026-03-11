namespace Domain.Order.Rules;

public sealed class ShippedOrderCannotBeCancelledRule(Aggregates.Order order) : IBusinessRule
{
    private readonly Aggregates.Order _order = order;

    public bool IsBroken()
    {
        return _order.IsShipped || _order.IsDelivered;
    }

    public string Message => _order.IsDelivered
        ? "سفارش تحویل داده شده قابل لغو نیست."
        : "سفارش ارسال شده قابل لغو نیست.";
}