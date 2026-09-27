using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record VariantCreatedEvent(
    VariantId VariantId,
    ProductId ProductId,
    Sku Sku,
    Money Price) : DomainEvent;
