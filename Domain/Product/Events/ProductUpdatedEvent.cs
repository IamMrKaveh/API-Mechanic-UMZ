namespace Domain.Product.Events;

public sealed class ProductUpdatedEvent : DomainEvent
{
    public int ProductId { get; }
    public string ProductName { get; }

    public ProductUpdatedEvent(int productId, string productName)
    {
        ProductId = productId;
        ProductName = productName;
    }
}