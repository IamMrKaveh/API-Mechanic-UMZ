using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderDeliveredEvent(OrderId orderId, UserId userId) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public UserId UserId { get; } = userId;
    public DateTime DeliveredAt { get; } = DateTime.UtcNow;
}