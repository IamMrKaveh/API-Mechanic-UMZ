using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderStatusChangedEvent(
    OrderId orderId,
    OrderNumber orderNumber,
    UserId userId,
    OrderStatusValue previousStatus,
    OrderStatusValue newStatus) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public OrderNumber OrderNumber { get; } = orderNumber;
    public UserId UserId { get; } = userId;
    public OrderStatusValue PreviousStatus { get; } = previousStatus;
    public OrderStatusValue NewStatus { get; } = newStatus;
}