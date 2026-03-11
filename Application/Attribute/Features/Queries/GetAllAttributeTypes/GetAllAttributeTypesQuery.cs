using Application.Common.Models;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public record GetAllAttributeTypesQuery : IRequest<ServiceResult<IEnumerable<AttributeTypeDto>>>;