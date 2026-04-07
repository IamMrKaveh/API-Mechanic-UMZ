using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockRestoredEvent(
    InventoryId inventoryId,
    VariantId variantId,
    int newStock,
    int restoredQuantity,
    string? reason = null) : DomainEvent
{
    public InventoryId InventoryId { get; } = inventoryId;
    public VariantId VariantId { get; } = variantId;
    public int NewStock { get; } = newStock;
    public int RestoredQuantity { get; } = restoredQuantity;
    public string? Reason { get; } = reason;
}