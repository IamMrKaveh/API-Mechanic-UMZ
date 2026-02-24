namespace Domain.Inventory.Events;

public class StockRestoredEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public int NewStock { get; }
    public int RestoredQuantity { get; }
    public string? Reason { get; }

    public StockRestoredEvent(int variantId, int productId, int newStock, int restoredQuantity = 0, string? reason = null)
    {
        VariantId = variantId;
        ProductId = productId;
        NewStock = newStock;
        RestoredQuantity = restoredQuantity;
        Reason = reason;
    }
}