namespace Domain.Variant.Events;

public sealed class ProductVariantRemovedEvent : DomainEvent
{
    public int ProductId { get; }
    public int VariantId { get; }

    public ProductVariantRemovedEvent(int productId, int variantId)
    {
        ProductId = productId;
        VariantId = variantId;
    }
}