using Domain.Common.Abstractions;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record ProductVariantShippingSetEvent(
    ProductVariantId VariantId,
    ProductId ProductId) : IDomainEvent;