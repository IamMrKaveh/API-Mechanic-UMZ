namespace Domain.Inventory;

public class InventoryTransaction : BaseEntity
{
    public int VariantId { get; set; }
    public Product.ProductVariant Variant { get; set; } = null!;

    public string TransactionType { get; set; } = string.Empty;

    public int QuantityChange { get; set; }

    public int StockBefore { get; set; }

    public int StockAfter => StockBefore + QuantityChange;

    public int? OrderItemId { get; set; }
    public Order.OrderItem? OrderItem { get; set; }

    public int? UserId { get; set; }
    public User.User? User { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
}