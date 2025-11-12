using DataAccessLayer.Models.Media;

namespace DataAccessLayer.Models.Category;

[Index(nameof(Name))]
public class TCategory : IAuditable, ISoftDeletable
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
    [MaxLength(200, ErrorMessage = "نام دسته‌بندی نمی‌تواند بیشتر از 200 کاراکتر باشد")]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<TMedia> Images { get; set; } = new List<TMedia>();
    public virtual ICollection<TCategoryGroup> CategoryGroups { get; set; } = new List<TCategoryGroup>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    [NotMapped]
    public string CacheKey => $"category_{Id}_{UpdatedAt?.Ticks}";
}