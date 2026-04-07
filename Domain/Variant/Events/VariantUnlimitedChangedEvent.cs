using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed class VariantUnlimitedChangedEvent(VariantId variantId, ProductId productId, bool isUnlimited) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public bool IsUnlimited { get; } = isUnlimited;
}