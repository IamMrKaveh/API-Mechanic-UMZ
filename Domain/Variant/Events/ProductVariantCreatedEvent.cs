namespace Domain.Variant.Events;

public sealed class ProductVariantCreatedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }

    public ProductVariantCreatedEvent(int variantId, int productId)
    {
        VariantId = variantId;
        ProductId = productId;
    }
}