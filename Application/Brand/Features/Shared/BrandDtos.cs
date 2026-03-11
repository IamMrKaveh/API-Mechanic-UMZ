namespace Application.Brand.Features.Shared;

public record BrandSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? IconUrl { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
    public int ActiveProductCount { get; init; }
}

public sealed record BrandDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public int ProductCount { get; init; }
}

public record BrandViewDto
{
    public int Id { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; }
    public string Name { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public string? IconUrl { get; init; }
    public int ActiveProductsCount { get; init; }
    public int TotalProductsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record BrandHierarchyDto
{
    public int Id { get; init; }
    public string Title { get; init; }
}

public record BrandDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
    public int ActiveProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record BrandListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? IconUrl { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record BrandTreeDto
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string? Slug { get; init; }
    public int SortOrder { get; init; }
    public int ProductCount { get; init; }
}

public record CreateBrandRequest(
    int CategoryId,

    [MaxLength(100)]
    string Name,

    [MaxLength(500)]
    string? Description,

    IFormFile? IconFile
);

public record UpdateBrandRequest(
    [Required]
    int CategoryId,

    [Required]
    [MaxLength(100)]
    string Name,

    [MaxLength(500)]
    string? Description,

    IFormFile? IconFile,

    [Required]
    string RowVersion
);

public record MoveBrandRequest(
    [Required]
    int SourceCategoryId,

    [Required]
    int TargetCategoryId,

    [Required]
    int BrandId
);