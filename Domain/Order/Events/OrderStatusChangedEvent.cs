using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderStatusChangedEvent : DomainEvent
{
    public OrderId OrderId { get; }
    public string OrderNumber { get; }
    public UserId UserId { get; }
    public string PreviousStatus { get; }
    public string NewStatus { get; }

    public OrderStatusChangedEvent(
        OrderId orderId,
        string orderNumber,
        UserId userId,
        string previousStatus,
        string newStatus)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        UserId = userId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        EventVersion = 1;
    }
}