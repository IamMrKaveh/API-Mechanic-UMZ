namespace Application.Attribute.Features.Shared;

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
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateAttributeTypeDto
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateAttributeValueDto
{
    public string Value { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public string? HexCode { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateAttributeValueDto
{
    public string? Value { get; set; }
    public string? DisplayValue { get; set; }
    public string? HexCode { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}