using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed class VariantAddedEvent(ProductId productId, VariantId variantId) : DomainEvent
{
    public ProductId ProductId { get; } = productId;
    public VariantId VariantId { get; } = variantId;
}