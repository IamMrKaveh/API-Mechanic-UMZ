using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockAdjustedEvent(
    InventoryId inventoryId,
    ProductVariantId variantId,
    int newQuantity,
    int adjustment,
    string reason) : DomainEvent
{
    public InventoryId InventoryId { get; } = inventoryId;
    public ProductVariantId VariantId { get; } = variantId;
    public int NewQuantity { get; } = newQuantity;
    public int Adjustment { get; } = adjustment;
    public string Reason { get; } = reason;
    public bool IsIncrease { get; } = adjustment > 0;
    public int PreviousQuantity => NewQuantity - Adjustment;
}