using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderStatusActivationChangedDomainEvent(
    OrderStatusId OrderStatusId,
    string Name,
    bool IsActive) : DomainEvent;
