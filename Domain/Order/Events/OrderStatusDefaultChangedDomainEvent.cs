using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderStatusDefaultChangedDomainEvent(
    OrderStatusId OrderStatusId,
    string Name,
    bool IsDefault) : DomainEvent;
