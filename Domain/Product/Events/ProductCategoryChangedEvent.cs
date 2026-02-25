namespace Domain.Product.Events;

public sealed class ProductCategoryChangedEvent : DomainEvent
{
    public int ProductId { get; }
    public int OldBrandId { get; }
    public int NewBrandId { get; }

    public ProductCategoryChangedEvent(int productId, int oldBrandId, int newBrandId)
    {
        ProductId = productId;
        OldBrandId = oldBrandId;
        NewBrandId = newBrandId;
    }
}