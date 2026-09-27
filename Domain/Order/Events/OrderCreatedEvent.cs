using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderCreatedEvent(
    OrderId OrderId,
    UserId UserId,
    OrderNumber OrderNumber,
    decimal FinalAmount,
    string Currency,
    int ItemsCount,
    Guid IdempotencyKey) : DomainEvent;
