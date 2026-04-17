namespace Application.Media.Features.Shared;

public record MediaDto
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
    public string? AltText { get; init; }
    public bool IsActive { get; init; }
    public string? PublicUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record SetPrimaryMediaDto(
    Guid MediaId
);

public sealed record ReorderMediaDto(
    string EntityType,
    int EntityId,
    ICollection<int> OrderedMediaIds
);