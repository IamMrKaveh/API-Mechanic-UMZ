using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderExpiredEvent(OrderId orderId, UserId userId, OrderNumber orderNumber) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public UserId UserId { get; } = userId;
    public OrderNumber OrderNumber { get; } = orderNumber;
    public DateTime ExpiredAt { get; } = DateTime.UtcNow;
}