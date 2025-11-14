namespace Domain.Inventory;

public class InventoryTransaction : IAuditable
{
    public int Id { get; set; }

    public int VariantId { get; set; }
    public Product.ProductVariant Variant { get; set; } = null!;

    public required string TransactionType { get; set; }

    public int Quantity { get; set; }

    public int StockBefore { get; set; }

    public int StockAfter { get; set; }

    public int? OrderItemId { get; set; }
    public Order.OrderItem? OrderItem { get; set; }

    public int? UserId { get; set; }
    public User.User? User { get; set; }

    public string? Notes { get; set; }

    public string? ReferenceNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}