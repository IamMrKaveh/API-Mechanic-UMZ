namespace Domain.Variant.Events;

public sealed class ProductVariantAddedEvent : DomainEvent
{
    public int ProductId { get; }
    public int VariantId { get; }

    public ProductVariantAddedEvent(int productId, int variantId)
    {
        ProductId = productId;
        VariantId = variantId;
    }
}