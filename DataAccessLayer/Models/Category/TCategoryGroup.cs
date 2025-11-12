using DataAccessLayer.Models.Media;

namespace DataAccessLayer.Models.Category;

[Index(nameof(CategoryId))]
[Index(nameof(Name))]
public class TCategoryGroup : IAuditable, ISoftDeletable
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام گروه دسته‌بندی الزامی است")]
    [MaxLength(200, ErrorMessage = "نام گروه دسته‌بندی نمی‌تواند بیشتر از 200 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    public virtual TCategory Category { get; set; } = null!;
    public int CategoryId { get; set; }

    public virtual ICollection<TProducts> Products { get; set; } = new List<TProducts>();
    public virtual ICollection<TMedia> Images { get; set; } = new List<TMedia>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    [NotMapped]
    public int ProductCount => Products?.Count ?? 0;

    [NotMapped]
    public int InStockProducts => Products?.Count(p => p.Variants != null && p.Variants.Any(v => v.IsUnlimited || v.Stock > 0)) ?? 0;

    [NotMapped]
    public long TotalValue => Products?.Sum(p => p.Variants?.Sum(v => (long)v.PurchasePrice * v.Stock) ?? 0) ?? 0;

    [NotMapped]
    public long TotalSellingValue => Products?.Sum(p => p.Variants?.Sum(v => (long)v.SellingPrice * v.Stock) ?? 0) ?? 0;
}