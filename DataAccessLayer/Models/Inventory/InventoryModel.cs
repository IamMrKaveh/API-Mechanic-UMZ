namespace DataAccessLayer.Models.Inventory;

[Index(nameof(VariantId), nameof(CreatedAt), nameof(TransactionType))]
[Index(nameof(OrderItemId))]
[Index(nameof(UserId))]
public class TInventoryTransaction : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int VariantId { get; set; }
    public TProductVariant Variant { get; set; } = null!;

    [Required, MaxLength(50)]
    public required string TransactionType { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public int StockBefore { get; set; }

    [Required]
    public int StockAfter { get; set; }

    public int? OrderItemId { get; set; }
    public TOrderItems? OrderItem { get; set; }

    public int? UserId { get; set; }
    public TUsers? User { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}