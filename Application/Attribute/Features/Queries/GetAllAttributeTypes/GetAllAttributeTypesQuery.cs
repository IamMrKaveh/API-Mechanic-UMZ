using Application.Attribute.Features.Shared;
using Application.Common.Results;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public record GetAllAttributeTypesQuery : IRequest<ServiceResult<IEnumerable<AttributeTypeDto>>>;