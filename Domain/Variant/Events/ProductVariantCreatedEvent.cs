using Domain.Common.Abstractions;
using Domain.Common.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record ProductVariantCreatedEvent(
    ProductVariantId VariantId,
    ProductId ProductId,
    string Sku,
    Money Price) : IDomainEvent;