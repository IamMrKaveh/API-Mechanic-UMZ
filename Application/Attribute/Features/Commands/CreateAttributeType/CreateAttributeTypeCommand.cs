using Application.Attribute.Features.Shared;
using Application.Common.Results;

namespace Application.Attribute.Features.Commands.CreateAttributeType;

public record CreateAttributeTypeCommand(
    string Name,
    string DisplayName,
    int SortOrder) : IRequest<ServiceResult<AttributeTypeDto>>;