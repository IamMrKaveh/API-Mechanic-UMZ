namespace Domain.Order.Events;

public sealed class OrderStatusChangedEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public int OldStatusId { get; }
    public int NewStatusId { get; }
    public string OldStatusName { get; }
    public string NewStatusName { get; }

    public OrderStatusChangedEvent(
        int orderId,
        int userId,
        int oldStatusId,
        int newStatusId,
        string oldStatusName,
        string newStatusName)
    {
        OrderId = orderId;
        UserId = userId;
        OldStatusId = oldStatusId;
        NewStatusId = newStatusId;
        OldStatusName = oldStatusName;
        NewStatusName = newStatusName;
    }
}