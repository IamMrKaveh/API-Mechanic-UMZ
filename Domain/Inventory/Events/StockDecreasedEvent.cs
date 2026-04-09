using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockDecreasedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int QuantityRemoved,
    int NewStockQuantity,
    string Reason = "") : DomainEvent
{
    public InventoryId InventoryId { get; } = InventoryId;
    public VariantId VariantId { get; } = VariantId;
    public int QuantityRemoved { get; } = QuantityRemoved;
    public int NewStockQuantity { get; } = NewStockQuantity;
    public string Reason { get; } = Reason;
}