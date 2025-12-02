namespace Application.DTOs;

public class CategoryGroupCreateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public IFormFile? IconFile { get; set; }
}

public class CategoryGroupUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public IFormFile? IconFile { get; set; }
    public string? RowVersion { get; set; }
}

public class CategoryGroupViewDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public string? IconUrl { get; set; }
    public int ProductCount { get; set; }
    public bool IsActive { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CategoryGroupHierarchyDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class CategoryGroupSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? IconUrl { get; set; }
    public int ProductCount { get; set; }
    public int InStockProducts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalSellingValue { get; set; }
}