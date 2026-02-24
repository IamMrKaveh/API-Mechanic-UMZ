namespace Domain.Order.Events;

public sealed class OrderShippedEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public DateTime ShippedAt { get; }

    public OrderShippedEvent(int orderId, int userId)
    {
        OrderId = orderId;
        UserId = userId;
        ShippedAt = DateTime.UtcNow;
    }
}