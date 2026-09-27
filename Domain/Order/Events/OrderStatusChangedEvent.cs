using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderStatusChangedEvent(
    OrderId OrderId,
    OrderNumber OrderNumber,
    UserId UserId,
    OrderStatusValue PreviousStatus,
    OrderStatusValue NewStatus) : DomainEvent;
