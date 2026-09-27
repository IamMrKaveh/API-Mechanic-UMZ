using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record VariantShippingSetEvent(
    VariantId VariantId,
    ProductId ProductId) : DomainEvent;
