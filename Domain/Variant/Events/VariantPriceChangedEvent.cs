using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed record ProductVariantPriceChangedEvent(
    VariantId VariantId,
    ProductId ProductId,
    Money PreviousPrice,
    Money NewPrice) : DomainEvent;
