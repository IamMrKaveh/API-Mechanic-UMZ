namespace Application.Media.Features.Shared;

/// <summary>
/// DTO ساده برای نمایش رسانه در لیست‌ها و سایر موجودیت‌ها
/// </summary>
public class MediaDto
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO جزئیات کامل رسانه (Admin)
/// </summary>
public class MediaDetailDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeDisplay { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// آیتم لیست رسانه‌ها (Admin)
/// </summary>
public class MediaListItemDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FileSizeDisplay { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SetPrimaryMediaRequestDto
{
    public int MediaId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
}