using Application.Common.Models;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public record GetAttributeTypeByIdQuery(
    int Id
    ) : IRequest<ServiceResult<AttributeTypeDto?>>;