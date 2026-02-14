namespace Application.Product.Features.Commands.CreateAttributeType;

public record CreateAttributeTypeCommand(string Name, string DisplayName, int SortOrder) : IRequest<ServiceResult<AttributeTypeDto>>;