using Domain.Common.Events;

namespace Domain.Order.Events;

public sealed class OrderStatusChangedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public Guid UserId { get; }
    public string PreviousStatus { get; }
    public string NewStatus { get; }

    public OrderStatusChangedEvent(
        Guid orderId,
        string orderNumber,
        Guid userId,
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