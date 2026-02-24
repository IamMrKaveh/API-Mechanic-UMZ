namespace Domain.Order.Rules;

public sealed class OrderStatusTransitionRule : IBusinessRule
{
    private readonly OrderStatusValue _currentStatus;
    private readonly OrderStatusValue _newStatus;

    public OrderStatusTransitionRule(OrderStatusValue currentStatus, OrderStatusValue newStatus)
    {
        _currentStatus = currentStatus;
        _newStatus = newStatus;
    }

    public bool IsBroken()
    {
        return !_currentStatus.CanTransitionTo(_newStatus);
    }

    public string Message =>
        $"امکان تغییر وضعیت از '{_currentStatus.DisplayName}' به '{_newStatus.DisplayName}' وجود ندارد.";
}