using Domain.Common.Abstractions;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record ProductVariantAttributeSetEvent(
    ProductVariantId VariantId,
    ProductId ProductId) : IDomainEvent;