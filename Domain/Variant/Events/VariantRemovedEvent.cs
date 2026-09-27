using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record VariantRemovedEvent(
    ProductId ProductId,
    VariantId VariantId) : DomainEvent;
