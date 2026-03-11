namespace Application.Category.Features.Shared;

public record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public string? IconUrl { get; init; }
    public int ActiveGroupsCount { get; init; }
    public int TotalProductsCount { get; init; }
    public List<BrandSummaryDto> Brands { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record CategoryDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public string? IconUrl { get; init; }
    public int ActiveGroupsCount { get; init; }
    public int TotalProductsCount { get; init; }
    public List<BrandSummaryDto> Brands { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
    public List<ProductSummaryDto> Products { get; init; } = [];
}

public record CategoryListItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? IconUrl { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int SortOrder { get; init; }
    public int GroupCount { get; init; }
    public int ActiveGroupCount { get; init; }
    public int TotalProductCount { get; init; }
    public int BrandCount { get; init; }
    public int ActiveBrandCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record CategoryTreeDto(
    int Id,
    string Name,
    string? Slug,
    string? IconUrl,
    int SortOrder,
    IReadOnlyList<BrandTreeDto> Brands
);

public record CategoryProductItemDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string? IconUrl { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public int TotalStock { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? BrandName { get; init; }
}

public record CategoryWithBrandsDto(
    int Id,
    string Name,
    string? Slug,
    string? Description,
    string? IconUrl,
    bool IsActive,
    bool IsDeleted,
    int SortOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? RowVersion,
    IReadOnlyList<BrandSummaryDto> Brands
);