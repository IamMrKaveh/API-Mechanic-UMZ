namespace Application.DTOs;

public class CategoryGroupCreateDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public IFormFile? IconFile { get; set; }
}

public class CategoryGroupUpdateDto
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public IFormFile? IconFile { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CategoryGroupViewDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public string? IconUrl { get; set; }
    public int ProductCount { get; set; }
    public bool IsActive { get; set; }
    public byte[]? RowVersion { get; set; }
}