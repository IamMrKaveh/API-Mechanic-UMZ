using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockSetUnlimitedEvent(
    InventoryId InventoryId,
    VariantId VariantId) : DomainEvent;
