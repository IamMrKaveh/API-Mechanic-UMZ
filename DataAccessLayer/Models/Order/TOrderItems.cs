namespace DataAccessLayer.Models.Order;

[Index(nameof(OrderId))]
[Index(nameof(VariantId))]
public class TOrderItems
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(OrderId))]
    public virtual TOrders Order { get; set; } = null!;
    public int OrderId { get; set; }

    [ForeignKey(nameof(VariantId))]
    public virtual TProductVariant Variant { get; set; } = null!;
    public int VariantId { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal SellingPrice { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Profit { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}