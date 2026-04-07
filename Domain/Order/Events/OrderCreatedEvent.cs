using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderCreatedEvent(
    OrderId orderId,
    UserId userId,
    OrderNumber orderNumber,
    decimal finalAmount,
    string currency,
    int itemsCount,
    Guid idempotencyKey) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public UserId UserId { get; } = userId;
    public OrderNumber OrderNumber { get; } = orderNumber;
    public decimal FinalAmount { get; } = finalAmount;
    public string Currency { get; } = currency;
    public int ItemsCount { get; } = itemsCount;
    public Guid IdempotencyKey { get; } = idempotencyKey;
}