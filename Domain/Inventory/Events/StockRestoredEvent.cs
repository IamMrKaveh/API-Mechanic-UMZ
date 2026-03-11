namespace Domain.Inventory.Events;

public class StockRestoredEvent(int variantId, int productId, int newStock, int restoredQuantity = 0, string? reason = null) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public int NewStock { get; } = newStock;
    public int RestoredQuantity { get; } = restoredQuantity;
    public string? Reason { get; } = reason;
}