namespace Application.DTOs;

public class AttributeTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public IEnumerable<AttributeValueSimpleDto> Values { get; set; } = [];
}

public class CreateAttributeTypeDto
{
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }

    [Required]
    [StringLength(50)]
    public required string DisplayName { get; set; }

    public int SortOrder { get; set; }
}

public class UpdateAttributeTypeDto
{
    [StringLength(50)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? DisplayName { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateAttributeValueDto
{
    [Required]
    [StringLength(100)]
    public required string Value { get; set; }

    [Required]
    [StringLength(100)]
    public required string DisplayValue { get; set; }

    [StringLength(7)]
    public string? HexCode { get; set; }

    public int SortOrder { get; set; }
}

public class UpdateAttributeValueDto
{
    [StringLength(100)]
    public string? Value { get; set; }

    [StringLength(100)]
    public string? DisplayValue { get; set; }

    [StringLength(7)]
    public string? HexCode { get; set; }

    public int? SortOrder { get; set; }

    public bool? IsActive { get; set; }
}