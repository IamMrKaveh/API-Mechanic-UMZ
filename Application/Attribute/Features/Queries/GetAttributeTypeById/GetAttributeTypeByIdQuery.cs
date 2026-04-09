using Application.Attribute.Features.Shared;
using Application.Common.Results;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public record GetAttributeTypeByIdQuery(Guid Id) : IRequest<ServiceResult<AttributeTypeDto?>>;