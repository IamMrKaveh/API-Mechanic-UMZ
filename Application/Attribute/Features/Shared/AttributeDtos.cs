namespace Application.Attribute.Features.Shared;

public record AttributeTypeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public List<AttributeValueDto> Values { get; init; } = [];
}

public record AttributeValueDto
{
    public Guid Id { get; init; }
    public Guid AttributeTypeId { get; init; }
    public string Value { get; init; } = string.Empty;
    public string DisplayValue { get; init; } = string.Empty;
    public string? HexCode { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}

public record UpdateAttributeTypeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
}