namespace DataAccessLayer.Models.Category;

[Index(nameof(Name))]
public class TCategory : BaseEntity
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    public ICollection<TMedia> Images { get; set; } = [];
    public ICollection<TCategoryGroup> CategoryGroups { get; set; } = [];
}

[Index(nameof(CategoryId), nameof(Name))]
public class TCategoryGroup : BaseEntity
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required]
    public int CategoryId { get; set; }
    public TCategory Category { get; set; } = null!;

    [Required]
    public bool IsActive { get; set; } = true;

    public ICollection<TProducts> Products { get; set; } = [];
    public ICollection<TMedia> Images { get; set; } = [];
}