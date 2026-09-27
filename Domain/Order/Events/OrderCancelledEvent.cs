using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderCancelledEvent(
    OrderId OrderId,
    OrderNumber OrderNumber,
    UserId UserId,
    string CancellationReason,
    bool WasPaid) : DomainEvent;
