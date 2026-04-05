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
    public string? HexCode { get; init; }
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

public sealed record AttributeValueDto(
    int Id,
    string TypeName,
    string TypeDisplayName,
    string Value,
    string DisplayValue,
    string? HexCode
);

public sealed record AttributeTypeWithValuesDto(
    int Id,
    string Name,
    string DisplayName,
    int SortOrder,
    bool IsActive,
    List<AttributeValueSimpleDto> Values
);

public sealed record AttributeValueSimpleDto(
    int Id,
    string Value,
    string DisplayValue,
    string? HexCode,
    int SortOrder,
    bool IsActive
);