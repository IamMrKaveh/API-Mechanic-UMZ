namespace Domain.Order.Rules;

public sealed class PaidOrderCannotBeDeletedRule : IBusinessRule
{
    private readonly Order _order;

    public PaidOrderCannotBeDeletedRule(Order order)
    {
        _order = order;
    }

    public bool IsBroken()
    {
        return _order.IsPaid;
    }

    public string Message => "سفارش پرداخت شده قابل حذف نیست.";
}