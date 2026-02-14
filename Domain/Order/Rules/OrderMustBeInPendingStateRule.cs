namespace Domain.Order.Rules;

public class OrderMustBeInPendingStateRule : IBusinessRule
{
    private readonly OrderStatusValue _currentStatus;

    public OrderMustBeInPendingStateRule(OrderStatusValue currentStatus)
    {
        _currentStatus = currentStatus;
    }

    public bool IsBroken() => _currentStatus != OrderStatusValue.Pending;

    public string Message => "Operation allowed only for Pending orders.";
}