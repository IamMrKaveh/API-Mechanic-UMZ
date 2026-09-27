using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockIncreasedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int QuantityAdded,
    int NewStockQuantity,
    string Reason = "") : DomainEvent;
