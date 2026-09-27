using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockDecreasedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int QuantityRemoved,
    int NewStockQuantity,
    string Reason = "") : DomainEvent;
