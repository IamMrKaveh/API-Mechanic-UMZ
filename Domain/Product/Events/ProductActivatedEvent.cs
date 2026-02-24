namespace Domain.Product.Events;

public sealed class ProductActivatedEvent : DomainEvent
{
    public int ProductId { get; }

    public ProductActivatedEvent(int productId)
    {
        ProductId = productId;
    }
}