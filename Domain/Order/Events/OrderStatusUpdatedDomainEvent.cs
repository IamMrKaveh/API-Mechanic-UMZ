using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderStatusUpdatedDomainEvent(
    OrderStatusId OrderStatusId,
    string Name,
    string DisplayName,
    int SortOrder) : DomainEvent;
