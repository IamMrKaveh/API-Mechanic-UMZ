using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderCancelledEvent(
    OrderId orderId,
    OrderNumber orderNumber,
    UserId userId,
    string cancellationReason,
    bool wasPaid) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public OrderNumber OrderNumber { get; } = orderNumber;
    public UserId UserId { get; } = userId;
    public string CancellationReason { get; } = cancellationReason;
    public bool WasPaid { get; } = wasPaid;
}