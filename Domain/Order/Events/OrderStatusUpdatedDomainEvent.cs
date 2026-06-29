using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderStatusUpdatedDomainEvent(
    OrderStatusId orderStatusId,
    string name,
    string displayName,
    int sortOrder) : DomainEvent
{
    public OrderStatusId OrderStatusId { get; } = orderStatusId;
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public int SortOrder { get; } = sortOrder;
}