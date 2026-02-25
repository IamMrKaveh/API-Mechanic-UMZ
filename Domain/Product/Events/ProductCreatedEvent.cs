namespace Domain.Product.Events;

public sealed class ProductCreatedEvent : DomainEvent
{
    public int ProductId { get; }
    public string ProductName { get; }
    public int BrandId { get; }

    public ProductCreatedEvent(int productId, string productName, int brandId)
    {
        ProductId = productId;
        ProductName = productName;
        BrandId = brandId;
    }
}