using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Product.Events;

public sealed record PriceChangedEvent(VariantId VariantId, ProductId ProductId, decimal OldPrice, decimal NewPrice, decimal? OldOriginalPrice, decimal? NewOriginalPrice) : DomainEvent;
