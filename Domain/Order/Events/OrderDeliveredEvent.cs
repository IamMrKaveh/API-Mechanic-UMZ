namespace Domain.Order.Events;

public sealed class OrderDeliveredEvent(int orderId, int userId) : DomainEvent
{
    public int OrderId { get; } = orderId;
    public int UserId { get; } = userId;
    public DateTime DeliveredAt { get; } = DateTime.UtcNow;
}