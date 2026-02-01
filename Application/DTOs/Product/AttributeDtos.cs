namespace Application.DTOs.Product;

public class AttributeTypeDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public int SortOrder { get; set; } public bool IsActive { get; set; } public IEnumerable<AttributeValueSimpleDto> Values { get; set; } = []; }

public class CreateAttributeTypeDto { public required string Name { get; set; } public required string DisplayName { get; set; } public int SortOrder { get; set; } }

public class UpdateAttributeTypeDto { public string? Name { get; set; } public string? DisplayName { get; set; } public int? SortOrder { get; set; } public bool? IsActive { get; set; } }

public class CreateAttributeValueDto { public required string Value { get; set; } public required string DisplayValue { get; set; } public string? HexCode { get; set; } public int SortOrder { get; set; } }

public class UpdateAttributeValueDto { public string? Value { get; set; } public string? DisplayValue { get; set; } public string? HexCode { get; set; } public int? SortOrder { get; set; } public bool? IsActive { get; set; } }