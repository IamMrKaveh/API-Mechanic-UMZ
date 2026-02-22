namespace Application.Media.Features.Shared;

public record MediaDto
{
    public int Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public int EntityId { get; init; }
    public string? AltText { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
    public string? Url { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record MediaDetailDto
{
    public int Id { get; init; }
    public string? Url { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string FileSizeDisplay { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public int EntityId { get; init; }
    public string? AltText { get; init; }
    public bool IsPrimary { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record MediaListItemDto
{
    public int Id { get; init; }
    public string? Url { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public string FileSizeDisplay { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public int EntityId { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SetPrimaryMediaRequestDto
{
    public int MediaId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public int EntityId { get; init; }
}

public record UploadImageInput(
    int? Id,
    string? AltText,
    int SortOrder,
    bool IsPrimary,
    long? FileSize = null,
    string? FileType = null
    );