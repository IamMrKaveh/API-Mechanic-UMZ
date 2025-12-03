namespace Application.DTOs;

public class CategoryHierarchyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public IEnumerable<CategoryGroupHierarchyDto> Groups { get; set; } = [];
}

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
    public string? RowVersion { get; set; }
}

public class CategoryViewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; }
    public byte[]? RowVersion { get; set; }
    public List<CategoryGroupSummaryDto> CategoryGroups { get; set; } = [];
}

public class CategoryDetailViewDto : CategoryViewDto
{
    public PagedResultDto<ProductSummaryDto> Products { get; set; } = new();
}