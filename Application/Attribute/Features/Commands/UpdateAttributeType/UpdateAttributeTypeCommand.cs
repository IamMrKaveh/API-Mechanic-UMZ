using Application.Attribute.Features.Shared;
using Application.Common.Results;

namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public record UpdateAttributeTypeCommand(
    int Id,
    string? Name,
    string? DisplayName,
    int? SortOrder,
    bool? IsActive) : IRequest<ServiceResult<UpdateAttributeTypeDto>>;