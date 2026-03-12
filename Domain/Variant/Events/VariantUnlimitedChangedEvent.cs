namespace Domain.Variant.Events;

public sealed class VariantUnlimitedChangedEvent(ProductVariantId variantId, ProductId productId, bool isUnlimited) : DomainEvent
{
    public ProductVariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public bool IsUnlimited { get; } = isUnlimited;
}