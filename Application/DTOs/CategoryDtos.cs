namespace Application.DTOs;

public class CategoryHierarchyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public IEnumerable<CategoryGroupHierarchyDto> Groups { get; set; } = [];
}

public class CategoryGroupHierarchyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class CategoryCreateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    public IFormFile? IconFile { get; set; }
}

public class CategoryUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    public IFormFile? IconFile { get; set; }
    public string? RowVersion { get; set; }
}

public class CategoryViewDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public byte[]? RowVersion { get; set; }
    public List<CategoryGroupSummaryDto> CategoryGroups { get; set; } = [];
}

public class CategoryDetailViewDto : CategoryViewDto
{
    public PagedResultDto<ProductSummaryDto> Products { get; set; } = new();
}

public class CategoryGroupSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? IconUrl { get; set; }
    public int ProductCount { get; set; }
    public int InStockProducts { get; set; }
    public long TotalValue { get; set; }
    public long TotalSellingValue { get; set; }
}