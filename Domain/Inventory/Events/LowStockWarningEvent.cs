using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public class LowStockWarningEvent(
    ProductVariantId variantId,
    ProductId productId,
    ProductName productName,
    int currentStock,
    int threshold) : DomainEvent
{
    public ProductVariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public ProductName ProductName { get; } = productName;
    public int CurrentStock { get; } = currentStock;
    public int Threshold { get; } = threshold;
    public int Shortage { get; } = threshold - currentStock;

    public bool IsCritical => CurrentStock == 0;
    public decimal StockPercentage => Threshold > 0 ? (decimal)CurrentStock / Threshold * 100 : 0;
}