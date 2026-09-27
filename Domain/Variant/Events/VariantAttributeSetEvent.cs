using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record VariantAttributeSetEvent(
    VariantId VariantId,
    ProductId ProductId) : DomainEvent;
