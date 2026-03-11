namespace Domain.Order.Rules;

public class OrderMustBeInPendingStateRule(OrderStatusValue currentStatus) : IBusinessRule
{
    private readonly OrderStatusValue _currentStatus = currentStatus;

    public bool IsBroken() => _currentStatus != OrderStatusValue.Pending;

    public string Message => "عملیات فقط برای سفارش‌های در انتظار پرداخت مجاز است.";
}