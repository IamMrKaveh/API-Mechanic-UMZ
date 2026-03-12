namespace Domain.Variant.Events;

public sealed class VariantStockChangedEvent(
    ProductVariantId variantId,
    ProductId productId,
    int quantityChanged) : DomainEvent
{
    public ProductVariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public int QuantityChanged { get; } = quantityChanged;
}