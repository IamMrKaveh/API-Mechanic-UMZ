using Domain.Common.Events;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderCreatedEvent : DomainEvent
{
    public OrderId OrderId { get; }
    public UserId UserId { get; }
    public string OrderNumber { get; }
    public decimal FinalAmount { get; }
    public string Currency { get; }
    public int ItemsCount { get; }
    public Guid IdempotencyKey { get; }

    public OrderCreatedEvent(
        OrderId orderId,
        UserId userId,
        string orderNumber,
        decimal finalAmount,
        string currency,
        int itemsCount,
        Guid idempotencyKey)
    {
        OrderId = orderId;
        UserId = userId;
        OrderNumber = orderNumber;
        FinalAmount = finalAmount;
        Currency = currency;
        ItemsCount = itemsCount;
        IdempotencyKey = idempotencyKey;
        EventVersion = 1;
    }
}