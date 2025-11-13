namespace DataAccessLayer.Models.Media;

[Index(nameof(EntityType), nameof(EntityId), nameof(SortOrder))]
[Index(nameof(IsPrimary))]
public class TMedia : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public required string FilePath { get; set; }

    [Required, MaxLength(255)]
    public required string FileName { get; set; }

    [Required, MaxLength(100)]
    public required string FileType { get; set; }

    [Required, Range(1, long.MaxValue)]
    public long FileSize { get; set; }

    [Required, MaxLength(50)]
    public required string EntityType { get; set; }

    [Required]
    public int EntityId { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int SortOrder { get; set; }

    [Required]
    public bool IsPrimary { get; set; }

    [MaxLength(255)]
    public string? AltText { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}