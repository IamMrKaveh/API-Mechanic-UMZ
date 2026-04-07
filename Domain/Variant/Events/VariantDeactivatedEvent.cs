using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed class VariantDeactivatedEvent(
    VariantId variantId,
    ProductId productId) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
}