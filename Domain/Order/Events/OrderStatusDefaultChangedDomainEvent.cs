using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderStatusDefaultChangedDomainEvent(
    OrderStatusId orderStatusId,
    string name,
    bool isDefault) : DomainEvent
{
    public OrderStatusId OrderStatusId { get; } = orderStatusId;
    public string Name { get; } = name;
    public bool IsDefault { get; } = isDefault;
}