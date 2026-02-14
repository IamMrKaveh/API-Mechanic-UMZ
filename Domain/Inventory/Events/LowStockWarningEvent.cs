namespace Domain.Inventory.Events;

public class LowStockWarningEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public string ProductName { get; }
    public int CurrentStock { get; }
    public int Threshold { get; }
    public int Shortage { get; }

    public LowStockWarningEvent(
        int variantId,
        int productId,
        string productName,
        int currentStock,
        int threshold)
    {
        VariantId = variantId;
        ProductId = productId;
        ProductName = productName;
        CurrentStock = currentStock;
        Threshold = threshold;
        Shortage = threshold - currentStock;
    }

    public bool IsCritical => CurrentStock == 0;
    public decimal StockPercentage => Threshold > 0 ? (decimal)CurrentStock / Threshold * 100 : 0;
}