using Application.Attribute.Features.Shared;

namespace Application.Attribute.Features.Commands.CreateAttributeValue;

public record CreateAttributeValueCommand(
    Guid TypeId,
    string Value,
    string DisplayValue,
    string? HexCode,
    int SortOrder) : IRequest<ServiceResult<AttributeValueDto>>;