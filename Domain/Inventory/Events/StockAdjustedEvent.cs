using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockAdjustedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int NewQuantity,
    int Adjustment,
    string Reason) : DomainEvent
{
    public bool IsIncrease { get; } = Adjustment > 0;
    public int PreviousQuantity => NewQuantity - Adjustment;
}