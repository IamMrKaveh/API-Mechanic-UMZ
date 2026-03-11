using Domain.Common.Abstractions;
using Domain.Common.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record ProductVariantPriceChangedEvent(
    ProductVariantId VariantId,
    ProductId ProductId,
    Money PreviousPrice,
    Money NewPrice) : IDomainEvent;