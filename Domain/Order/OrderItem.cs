namespace Domain.Order;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int VariantId { get; set; }
    public Product.ProductVariant Variant { get; set; } = null!;

    public decimal PurchasePrice { get; set; }

    public decimal SellingPrice { get; set; }

    public int Quantity { get; set; }

    public decimal Amount { get; set; }

    public decimal Profit { get; set; }

    public byte[]? RowVersion { get; set; }

    public ICollection<Inventory.InventoryTransaction> InventoryTransactions { get; set; } = [];

    public void RecalculateTotals()
    {
        Amount = Quantity * SellingPrice;
        Profit = Quantity * (SellingPrice - PurchasePrice);
    }
}