using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockReleasedEvent(VariantId VariantId, ProductId ProductId, int Quantity, string? Reason = null) : DomainEvent;