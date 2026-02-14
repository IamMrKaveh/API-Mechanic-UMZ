namespace Domain.Product.Events;

public sealed class ProductDeletedEvent : DomainEvent
{
    public int ProductId { get; }
    public int? DeletedBy { get; }

    public ProductDeletedEvent(int productId, int? deletedBy)
    {
        ProductId = productId;
        DeletedBy = deletedBy;
    }
}