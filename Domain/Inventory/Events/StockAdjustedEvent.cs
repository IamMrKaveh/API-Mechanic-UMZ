namespace Domain.Inventory.Events;

public class AdjustStockEvent(int variantId, int newQuantity, int adjustment) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int NewQuantity { get; } = newQuantity;
    public int Adjustment { get; } = adjustment;
    public bool IsIncrease { get; } = adjustment > 0;

    public int PreviousQuantity => NewQuantity - Adjustment;
}