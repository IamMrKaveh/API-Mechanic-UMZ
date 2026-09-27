using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockRestoredEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int NewStock,
    int RestoredQuantity,
    string? Reason = null) : DomainEvent;
