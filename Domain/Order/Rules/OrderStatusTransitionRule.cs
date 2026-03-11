namespace Domain.Order.Rules;

public sealed class OrderStatusTransitionRule(OrderStatusValue currentStatus, OrderStatusValue newStatus) : IBusinessRule
{
    private readonly OrderStatusValue _currentStatus = currentStatus;
    private readonly OrderStatusValue _newStatus = newStatus;

    public bool IsBroken()
    {
        return !_currentStatus.CanTransitionTo(_newStatus);
    }

    public string Message =>
        $"امکان تغییر وضعیت از '{_currentStatus.DisplayName}' به '{_newStatus.DisplayName}' وجود ندارد.";
}