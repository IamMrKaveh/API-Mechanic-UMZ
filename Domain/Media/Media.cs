namespace Domain.Media;

public class Media : IAuditable, ISoftDeletable, IActivatable
{
    public int Id { get; set; }

    public required string FilePath { get; set; }

    public required string FileName { get; set; }

    public required string FileType { get; set; }

    public long FileSize { get; set; }

    public required string EntityType { get; set; }

    public int EntityId { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public string? AltText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public bool IsActive { get; set; } = true;
}