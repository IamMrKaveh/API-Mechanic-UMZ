namespace Presentation.Attribute.Requests;

public record CreateAttributeTypeRequest(
    string Name,
    string DisplayName,
    int SortOrder,
    bool IsActive = true);

public record UpdateAttributeTypeRequest(
    string Name,
    string DisplayName,
    int SortOrder,
    bool IsActive);

public record CreateAttributeValueRequest(
    string Value,
    string DisplayValue,
    string? HexCode = null,
    int SortOrder = 0);

public record UpdateAttributeValueRequest(
    string Value,
    string DisplayValue,
    string? HexCode,
    int SortOrder,
    bool IsActive);