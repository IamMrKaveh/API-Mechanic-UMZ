using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed class VariantStockChangedEvent(
    VariantId variantId,
    ProductId productId,
    int quantityChanged) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public int QuantityChanged { get; } = quantityChanged;
}