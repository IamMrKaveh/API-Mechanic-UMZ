namespace DataAccessLayer.Models.Media;

[Index(nameof(EntityType), nameof(EntityId))]
[Index(nameof(CreatedAt))]
public class TMedia : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FileType { get; set; } = string.Empty; // image/jpeg, image/png

    public long FileSize { get; set; }

    [Required, MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // Product, Category, etc.

    public int EntityId { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsPrimary { get; set; } = false;

    [MaxLength(255)]
    public string? AltText { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}