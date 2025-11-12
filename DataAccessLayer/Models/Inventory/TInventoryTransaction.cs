namespace DataAccessLayer.Models.Inventory;

[Index(nameof(VariantId), nameof(CreatedAt))]
[Index(nameof(TransactionType))]
public class TInventoryTransaction : IAuditable
{
    [Key]
    public int Id { get; set; }

    public int VariantId { get; set; }
    public virtual TProductVariant Variant { get; set; } = null!;

    [Required, MaxLength(50)]
    public string TransactionType { get; set; } = string.Empty;
    // Purchase, Sale, Return, Adjustment, Damaged

    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }

    public int? OrderItemId { get; set; }
    public virtual TOrderItems? OrderItem { get; set; }

    public int? UserId { get; set; }
    public virtual TUsers? User { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}