namespace Domain.Inventory.Events;

public class LowStockWarningEvent(
    int variantId,
    int productId,
    string productName,
    int currentStock,
    int threshold) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public string ProductName { get; } = productName;
    public int CurrentStock { get; } = currentStock;
    public int Threshold { get; } = threshold;
    public int Shortage { get; } = threshold - currentStock;

    public bool IsCritical => CurrentStock == 0;
    public decimal StockPercentage => Threshold > 0 ? (decimal)CurrentStock / Threshold * 100 : 0;
}