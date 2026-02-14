namespace Domain.Inventory.Events;

public class AdjustStockEvent : DomainEvent
{
    public int VariantId { get; }
    public int NewQuantity { get; }
    public int Adjustment { get; }
    public bool IsIncrease { get; }

    public AdjustStockEvent(int variantId, int newQuantity, int adjustment)
    {
        VariantId = variantId;
        NewQuantity = newQuantity;
        Adjustment = adjustment;
        IsIncrease = adjustment > 0;
    }

    public int PreviousQuantity => NewQuantity - Adjustment;
}