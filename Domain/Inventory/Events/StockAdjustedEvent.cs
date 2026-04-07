using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockAdjustedEvent(
    InventoryId inventoryId,
    VariantId variantId,
    int newQuantity,
    int adjustment,
    string reason) : DomainEvent
{
    public InventoryId InventoryId { get; } = inventoryId;
    public VariantId VariantId { get; } = variantId;
    public int NewQuantity { get; } = newQuantity;
    public int Adjustment { get; } = adjustment;
    public string Reason { get; } = reason;
    public bool IsIncrease { get; } = adjustment > 0;
    public int PreviousQuantity => NewQuantity - Adjustment;
}