namespace Domain.Variant.Events;

public sealed class ProductVariantAddedEvent(int productId, int variantId) : DomainEvent
{
    public int ProductId { get; } = productId;
    public int VariantId { get; } = variantId;
}