using Domain.Common.Events;
using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderStatusDeletedDomainEvent(
    OrderStatusId orderStatusId,
    string name) : DomainEvent
{
    public OrderStatusId OrderStatusId { get; } = orderStatusId;
    public string Name { get; } = name;
}