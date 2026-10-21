using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderItemRemovedEvent(
    OrderId OrderId,
    OrderNumber OrderNumber,
    UserId UserId,
    OrderItemId OrderItemId,
    decimal RemovedAmount) : DomainEvent;
