namespace Domain.Variant.Events;

public sealed class ProductVariantRemovedEvent(int productId, int variantId) : DomainEvent
{
    public int ProductId { get; } = productId;
    public int VariantId { get; } = variantId;
}