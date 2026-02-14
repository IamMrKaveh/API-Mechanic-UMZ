namespace Application.Product.Features.Commands.CreateAttributeValue;

public record CreateAttributeValueCommand(int TypeId, string Value, string DisplayValue, string? HexCode, int SortOrder) : IRequest<ServiceResult<AttributeValueDto>>;