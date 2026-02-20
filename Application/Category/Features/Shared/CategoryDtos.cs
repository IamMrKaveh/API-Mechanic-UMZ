namespace Application.Category.Features.Shared;

public class CategoryHierarchyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public IEnumerable<BrandHierarchyDto> Groups { get; set; } = [];
}

public class CategoryCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class CategoryUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public string? RowVersion { get; set; }
}

public class CategoryViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? IconUrl { get; set; }
    public int ActiveGroupsCount { get; set; }
    public int TotalProductsCount { get; set; }
    public List<BrandSummaryDto> Brands { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
}

public class CategoryDetailViewDto : CategoryViewDto
{
    public List<ProductSummaryDto> Products { get; set; } = new();
}

/// <summary>
/// آیتم لیست دسته‌بندی‌ها (Admin)
/// </summary>
public class CategoryListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int SortOrder { get; set; }
    public int GroupCount { get; set; }
    public int ActiveGroupCount { get; set; }
    public int TotalProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RowVersion { get; set; }
}

/// <summary>
/// ساختار درختی برای منو - حداقل داده مورد نیاز
/// </summary>
public class CategoryTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; }
    public IReadOnlyList<BrandTreeDto> Groups { get; set; } = [];
}

/// <summary>
/// محصول در لیست محصولات دسته‌بندی
/// </summary>
public class CategoryProductItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? IconUrl { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalStock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// جزئیات دسته‌بندی به همراه گروه‌ها (Admin)
/// </summary>
public class CategoryWithGroupsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RowVersion { get; set; }
    public IReadOnlyList<BrandSummaryDto> Groups { get; set; } = [];
}