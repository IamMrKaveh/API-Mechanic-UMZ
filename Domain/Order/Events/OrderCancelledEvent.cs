using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderCancelledEvent : DomainEvent
{
    public OrderId OrderId { get; }
    public string OrderNumber { get; }
    public UserId UserId { get; }
    public string CancellationReason { get; }
    public bool WasPaid { get; }

    public OrderCancelledEvent(
        OrderId orderId,
        string orderNumber,
        UserId userId,
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