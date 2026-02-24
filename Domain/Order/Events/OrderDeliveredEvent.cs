namespace Domain.Order.Events;

public sealed class OrderDeliveredEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public DateTime DeliveredAt { get; }

    public OrderDeliveredEvent(int orderId, int userId)
    {
        OrderId = orderId;
        UserId = userId;
        DeliveredAt = DateTime.UtcNow;
    }
}