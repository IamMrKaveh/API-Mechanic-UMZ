using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderExpiredEvent(OrderId orderId) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
}