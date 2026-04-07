using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderShippedEvent(OrderId orderId, UserId userId) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public UserId UserId { get; } = userId;
    public DateTime ShippedAt { get; } = DateTime.UtcNow;
}