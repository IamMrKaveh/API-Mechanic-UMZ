namespace Domain.Product.Events;

public sealed class ProductCategoryChangedEvent : DomainEvent
{
    public int ProductId { get; }
    public int OldCategoryGroupId { get; }
    public int NewCategoryGroupId { get; }

    public ProductCategoryChangedEvent(int productId, int oldCategoryGroupId, int newCategoryGroupId)
    {
        ProductId = productId;
        OldCategoryGroupId = oldCategoryGroupId;
        NewCategoryGroupId = newCategoryGroupId;
    }
}