using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed class VariantCreatedEvent(
    VariantId variantId,
    ProductId productId,
    Sku sku,
    Money price) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public Sku Sku { get; } = sku;
    public Money Price { get; } = price;
}