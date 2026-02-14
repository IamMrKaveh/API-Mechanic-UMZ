namespace Domain.Product.Events;

public sealed class ProductCreatedEvent : DomainEvent
{
    public int ProductId { get; }
    public string ProductName { get; }
    public int CategoryGroupId { get; }

    public ProductCreatedEvent(int productId, string productName, int categoryGroupId)
    {
        ProductId = productId;
        ProductName = productName;
        CategoryGroupId = categoryGroupId;
    }
}