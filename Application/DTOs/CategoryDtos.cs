namespace Application.DTOs;

public class CategoryCreateDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    public IFormFile? IconFile { get; set; }
}

public class CategoryUpdateDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    public IFormFile? IconFile { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CategoryViewDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
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
    public required string Name { get; set; }
    public string? IconUrl { get; set; }
    public int ProductCount { get; set; }
    public int InStockProducts { get; set; }
    public long TotalValue { get; set; }
    public long TotalSellingValue { get; set; }
}