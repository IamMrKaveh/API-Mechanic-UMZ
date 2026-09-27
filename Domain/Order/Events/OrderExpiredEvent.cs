using Domain.Order.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderExpiredEvent(OrderId OrderId) : DomainEvent;
