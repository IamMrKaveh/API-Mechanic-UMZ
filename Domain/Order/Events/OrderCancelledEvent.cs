using Domain.Common.Events;

namespace Domain.Order.Events;

public sealed class OrderCancelledEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public Guid UserId { get; }
    public string CancellationReason { get; }
    public bool WasPaid { get; }

    public OrderCancelledEvent(
        Guid orderId,
        string orderNumber,
        Guid userId,
        string cancellationReason,
        bool wasPaid)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        UserId = userId;
        CancellationReason = cancellationReason;
        WasPaid = wasPaid;
        EventVersion = 1;
    }
}