namespace Domain.Media;

public class Media : IAuditable
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
}