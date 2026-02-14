namespace Domain.Order.Rules;

public sealed class OrderCanBeModifiedRule : IBusinessRule
{
    private readonly Order _order;

    public OrderCanBeModifiedRule(Order order)
    {
        _order = order;
    }

    public bool IsBroken()
    {
        return !_order.CanBeModified();
    }

    public string Message
    {
        get
        {
            if (_order.IsDeleted) return "سفارش حذف شده قابل ویرایش نیست.";
            if (_order.IsPaid) return "سفارش پرداخت شده قابل ویرایش نیست.";
            if (_order.IsCancelled) return "سفارش لغو شده قابل ویرایش نیست.";
            return "این سفارش قابل ویرایش نیست.";
        }
    }
}