namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public record UpdateAttributeValueCommand(
    Guid Id,
    string? Value,
    string? DisplayValue,
    string? HexCode,
    int? SortOrder,
    bool? IsActive) : IRequest<ServiceResult>;