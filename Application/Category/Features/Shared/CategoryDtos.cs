namespace Application.Category.Features.Shared;

public record CategoryHierarchyDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public IEnumerable<BrandHierarchyDto> Brands { get; init; } = [];
}

public record CategoryCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
}

public record CategoryUpdateDto
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public string? RowVersion { get; init; }
}

public record CategoryViewDto
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
    public List<BrandSummaryDto> Brands { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public string? RowVersion { get; init; }
}

public record CategoryDetailViewDto : CategoryViewDto
{
    public List<ProductSummaryDto> Products { get; init; } = new();
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
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
    public int? brandCount { get; init; }
    public int? ActivebrandCount { get; init; }
}

public record CategoryTreeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? IconUrl { get; init; }
    public int SortOrder { get; init; }
    public IReadOnlyList<BrandTreeDto> Brands { get; init; } = [];
}

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
    public string? brandName { get; init; }
}

public record CategoryWithBrandsDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public string? IconUrl { get; init; }
    public bool IsActive { get; init; }
    public bool IsDeleted { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? RowVersion { get; init; }
    public IReadOnlyList<BrandSummaryDto> Brands { get; init; } = [];
}