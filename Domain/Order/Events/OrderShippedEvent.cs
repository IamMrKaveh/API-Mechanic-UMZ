namespace Domain.Order.Events;

public sealed class OrderShippedEvent(int orderId, int userId) : DomainEvent
{
    public int OrderId { get; } = orderId;
    public int UserId { get; } = userId;
    public DateTime ShippedAt { get; } = DateTime.UtcNow;
}