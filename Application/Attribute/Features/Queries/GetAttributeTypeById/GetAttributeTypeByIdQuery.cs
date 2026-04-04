using Application.Attribute.Features.Shared;
using Application.Common.Results;

namespace Application.Attribute.Features.Queries.GetAttributeTypeById;

public record GetAttributeTypeByIdQuery(int Id) : IRequest<ServiceResult<AttributeTypeDto?>>;