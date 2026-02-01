namespace Domain.Events;

public class OrderCreatedEvent : IDomainEvent
{
    public Order.Order Order { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public OrderCreatedEvent(Order.Order order)
    {
        Order = order;
    }
}