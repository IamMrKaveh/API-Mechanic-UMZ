using DataAccessLayer.Models.Media;

namespace DataAccessLayer.Models.Product;

[Index(nameof(CategoryGroupId), nameof(IsActive), nameof(CreatedAt))]
[Index(nameof(IsActive), nameof(Name))]
[Index(nameof(Sku), IsUnique = true)]
public class TProducts : IAuditable, ISoftDeletable
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Sku { get; set; }

    public int CategoryGroupId { get; set; }
    public virtual TCategoryGroup CategoryGroup { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<TProductVariant> Variants { get; set; } = new List<TProductVariant>();
    public virtual ICollection<TOrderItems>? OrderDetails { get; set; } = new List<TOrderItems>();
    public virtual ICollection<TMedia> Images { get; set; } = new List<TMedia>();
    public virtual ICollection<TProductReview> Reviews { get; set; } = new List<TProductReview>();

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    public int TotalStock { get; set; }

    [NotMapped]
    public bool HasMultipleVariants => Variants?.Count(v => v.IsActive) > 1;
}