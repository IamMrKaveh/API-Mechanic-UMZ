namespace Domain.Variant.Events;

public sealed class ProductVariantRemovedEvent(ProductId productId, ProductVariantId variantId) : DomainEvent
{
    public ProductId ProductId { get; } = productId;
    public ProductVariantId VariantId { get; } = variantId;
}