namespace Domain.Product.Events;

public sealed class ProductDeactivatedEvent : DomainEvent
{
    public int ProductId { get; }

    public ProductDeactivatedEvent(int productId)
    {
        ProductId = productId;
    }
}