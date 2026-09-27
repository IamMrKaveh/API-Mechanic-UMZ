using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderStatusCreatedDomainEvent(
    OrderStatusId OrderStatusId,
    string Name,
    string DisplayName,
    int SortOrder) : DomainEvent;
