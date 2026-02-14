namespace Domain.Order.Rules;

public sealed class OrderCanBeCancelledRule : IBusinessRule
{
    private readonly Order _order;

    public OrderCanBeCancelledRule(Order order)
    {
        _order = order;
    }

    public bool IsBroken()
    {
        return !_order.CanBeCancelled();
    }

    public string Message
    {
        get
        {
            if (_order.IsDeleted) return "سفارش حذف شده است.";
            if (_order.IsShipped) return "سفارش ارسال شده قابل لغو نیست.";
            if (_order.IsDelivered) return "سفارش تحویل داده شده قابل لغو نیست.";
            if (_order.IsCancelled) return "سفارش قبلاً لغو شده است.";
            return "این سفارش قابل لغو نیست.";
        }
    }
}