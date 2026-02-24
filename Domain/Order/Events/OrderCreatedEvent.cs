namespace Domain.Order.Events;

public sealed class OrderCreatedEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public decimal FinalAmount { get; }
    public string IdempotencyKey { get; }
    public string OrderNumber { get; }
    public int ItemsCount { get; }

    public OrderCreatedEvent(Order order)
    {
        OrderId = order.Id;
        UserId = order.UserId;
        FinalAmount = order.FinalAmount.Amount;
        IdempotencyKey = order.IdempotencyKey;
        OrderNumber = order.OrderNumber;
        ItemsCount = order.OrderItems.Count;
    }
}