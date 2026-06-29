using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderStatusActivationChangedDomainEvent(
    OrderStatusId orderStatusId,
    string name,
    bool isActive) : DomainEvent
{
    public OrderStatusId OrderStatusId { get; } = orderStatusId;
    public string Name { get; } = name;
    public bool IsActive { get; } = isActive;
}