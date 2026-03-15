using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed class ProductVariantAddedEvent(ProductId productId, ProductVariantId variantId) : DomainEvent
{
    public ProductId ProductId { get; } = productId;
    public ProductVariantId VariantId { get; } = variantId;
}