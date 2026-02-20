using Application.Attribute.Features.Shared;

namespace Application.Attribute.Features.Commands.CreateAttributeType;

public record CreateAttributeTypeCommand(string Name, string DisplayName, int SortOrder) : IRequest<ServiceResult<AttributeTypeDto>>;