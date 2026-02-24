namespace Application.Attribute.Features.Shared;

public record AttributeTypeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public IEnumerable<AttributeValueSimpleDto> Values { get; init; } = [];
}

public record CreateAttributeTypeDto
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public record UpdateAttributeTypeDto
{
    public string? Name { get; init; }
    public string? DisplayName { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsActive { get; init; }
}

public record CreateAttributeValueDto
{
    public string Value { get; init; } = string.Empty;
    public string DisplayValue { get; init; } = string.Empty;
    public string? HexCode { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public record UpdateAttributeValueDto
{
    public string? Value { get; init; }
    public string? DisplayValue { get; init; }
    public string? HexCode { get; init; }
    public int? SortOrder { get; init; }
    public bool? IsActive { get; init; }
}