using DataAccessLayer.Models.Inventory;
using DataAccessLayer.Models.Media;

namespace DataAccessLayer.Models.Product;

[Index(nameof(ProductId), nameof(IsActive), nameof(Stock))]
[Index(nameof(Sku), IsUnique = true)]
[Index(nameof(IsActive))]
public class TProductVariant : IAuditable, ISoftDeletable
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual TProducts Product { get; set; } = null!;
    public int ProductId { get; set; }

    [StringLength(100)]
    public string? Sku { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    [Range(0.01, double.MaxValue)]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    [Range(0, double.MaxValue)]
    public decimal OriginalPrice { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    [Range(0.01, double.MaxValue)]
    public decimal SellingPrice { get; set; }

    public int Stock { get; set; } = 0;
    public bool IsUnlimited { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public int? MinOrderQuantity { get; set; }
    public int? MaxOrderQuantity { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<TProductVariantAttribute> VariantAttributes { get; set; } = new List<TProductVariantAttribute>();
    public virtual ICollection<TMedia> Images { get; set; } = new List<TMedia>();
    public virtual ICollection<TInventoryTransaction> InventoryTransactions { get; set; } = new List<TInventoryTransaction>();

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    [NotMapped]
    public double DiscountPercentage => OriginalPrice > 0
        ? Math.Round((double)((OriginalPrice - SellingPrice) / OriginalPrice * 100), 2)
        : 0;

    [NotMapped]
    public bool HasDiscount => OriginalPrice > SellingPrice;

    [NotMapped]
    public bool IsInStock => IsUnlimited || Stock > 0;

    [NotMapped]
    public string DisplayName
    {
        get
        {
            var attributes = VariantAttributes?
                .OrderBy(va => va.AttributeValue.AttributeType.SortOrder)
                .Select(va => va.AttributeValue.DisplayValue)
                .ToList();

            return attributes?.Any() == true
                ? $"{Product?.Name} - {string.Join(" / ", attributes)}"
                : Product?.Name ?? "";
        }
    }
}