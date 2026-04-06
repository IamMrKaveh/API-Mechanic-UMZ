namespace Application.Category.Features.Shared;

public record CategoryDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int? ParentId { get; init; }
    public string? ParentName { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
    public int BrandCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
    public List<CategoryTreeDto> Children { get; init; } = [];
}

public record CategoryListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public int? ParentId { get; init; }
    public string? ParentName { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record CategorySummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? ImageUrl { get; init; }
}

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public bool IsRootCategory { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CategoryTreeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public List<CategoryTreeDto> Children { get; init; } = [];
}