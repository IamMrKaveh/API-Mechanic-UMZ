namespace Domain.Order.Rules;

public sealed class PaidOrderCannotBeDeletedRule(Aggregates.Order order) : IBusinessRule
{
    private readonly Aggregates.Order _order = order;

    public bool IsBroken()
    {
        return _order.IsPaid;
    }

    public string Message => "سفارش پرداخت شده قابل حذف نیست.";
}