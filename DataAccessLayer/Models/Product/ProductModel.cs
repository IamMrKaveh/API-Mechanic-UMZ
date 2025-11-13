namespace DataAccessLayer.Models.Product;

[Index(nameof(CategoryGroupId), nameof(IsActive), nameof(CreatedAt))]
[Index(nameof(Sku), IsUnique = true)]
[Index(nameof(Name))]
public class TProducts : BaseEntity
{
    [Required, StringLength(200)]
    public required string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Sku { get; set; }

    [Required]
    public int CategoryGroupId { get; set; }
    public TCategoryGroup CategoryGroup { get; set; } = null!;

    [Required]
    public bool IsActive { get; set; } = true;

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal MinPrice { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal MaxPrice { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int TotalStock { get; set; }

    [NotMapped]
    public bool HasMultipleVariants => Variants.Count > 1;

    public ICollection<TProductVariant> Variants { get; set; } = [];
    public ICollection<TOrderItems> OrderDetails { get; set; } = [];
    public ICollection<TMedia> Images { get; set; } = [];
    public ICollection<TProductReview> Reviews { get; set; } = [];
}

[Index(nameof(ProductId), nameof(IsActive), nameof(Stock))]
[Index(nameof(Sku), IsUnique = true)]
[Index(nameof(IsUnlimited))]
public class TProductVariant : BaseEntity
{
    [Required]
    public int ProductId { get; set; }
    public TProducts Product { get; set; } = null!;

    [StringLength(100)]
    public string? Sku { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal PurchasePrice { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal OriginalPrice { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal SellingPrice { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int Stock { get; set; }

    [Required]
    public bool IsUnlimited { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int? MinOrderQuantity { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxOrderQuantity { get; set; }

    [NotMapped]
    public string DisplayName
    {
        get
        {
            var attributes = string.Join(" / ", VariantAttributes
                .OrderBy(a => a.AttributeValue.AttributeType.SortOrder)
                .Select(a => a.AttributeValue.DisplayValue));
            return string.IsNullOrWhiteSpace(attributes) ? Product.Name : $"{Product.Name} ({attributes})";
        }
    }

    [NotMapped]
    public bool IsInStock => IsUnlimited || Stock > 0;

    [NotMapped]
    public bool HasDiscount => OriginalPrice > SellingPrice;

    [NotMapped]
    public double DiscountPercentage => OriginalPrice > 0 ? Math.Round((double)(1 - (SellingPrice / OriginalPrice)) * 100, 2) : 0;

    public ICollection<TProductVariantAttribute> VariantAttributes { get; set; } = [];
    public ICollection<TMedia> Images { get; set; } = [];
    public ICollection<TInventoryTransaction> InventoryTransactions { get; set; } = [];
    public ICollection<TCartItems> CartItems { get; set; } = [];
}

[Index(nameof(ProductId), nameof(CreatedAt))]
[Index(nameof(UserId), nameof(Status))]
[Index(nameof(Status), nameof(IsVerifiedPurchase))]
[Index(nameof(Rating))]
public class TProductReview : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }
    public TProducts Product { get; set; } = null!;

    [Required]
    public int UserId { get; set; }
    public TUsers User { get; set; } = null!;

    public int? OrderId { get; set; }
    public TOrders? Order { get; set; }

    [Required, Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Required]
    public bool IsVerifiedPurchase { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int LikeCount { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int DislikeCount { get; set; }

    [MaxLength(500)]
    public string? AdminReply { get; set; }

    public DateTime? RepliedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}