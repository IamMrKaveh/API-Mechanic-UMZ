namespace Application.Brand.Features.Shared;

public class BrandCreateDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class BrandUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? RowVersion { get; set; }
}

public class BrandViewDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? IconUrl { get; set; }
    public int ActiveProductsCount { get; set; }
    public int TotalProductsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
}

public class BrandHierarchyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// جزئیات یک Brand (Admin)
/// </summary>
public class BrandDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int SortOrder { get; set; }
    public int ProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RowVersion { get; set; }
}

/// <summary>
/// آیتم لیست گروه‌ها (Admin)
/// </summary>
public class BrandListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? IconUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int SortOrder { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
}

public class BrandTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int SortOrder { get; set; }
    public int ProductCount { get; set; }
}

public class BrandSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int ProductCount { get; set; }
    public int ActiveProductCount { get; set; }
}