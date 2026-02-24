namespace Domain.Order.Events;

public sealed class OrderCancelledEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public string Reason { get; }

    public OrderCancelledEvent(int orderId, int userId, string reason)
    {
        OrderId = orderId;
        UserId = userId;
        Reason = reason;
    }
}