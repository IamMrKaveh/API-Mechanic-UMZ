using Domain.Common.Events;

namespace Domain.Order.Events;

public sealed class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public string OrderNumber { get; }
    public decimal FinalAmount { get; }
    public string Currency { get; }
    public int ItemsCount { get; }
    public Guid IdempotencyKey { get; }

    public OrderCreatedEvent(
        Guid orderId,
        Guid userId,
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