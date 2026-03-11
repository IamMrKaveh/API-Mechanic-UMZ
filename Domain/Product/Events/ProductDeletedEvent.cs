namespace Domain.Product.Events;

public sealed class ProductDeletedEvent(int productId, int? deletedBy) : DomainEvent
{
    public int ProductId { get; } = productId;
    public int? DeletedBy { get; } = deletedBy;
}