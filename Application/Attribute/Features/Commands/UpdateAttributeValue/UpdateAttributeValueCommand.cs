namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public record UpdateAttributeValueCommand(
    int Id,
    string? Value,
    string? DisplayValue,
    string? HexCode,
    int? SortOrder,
    bool? IsActive
    ) : IRequest<ServiceResult>;