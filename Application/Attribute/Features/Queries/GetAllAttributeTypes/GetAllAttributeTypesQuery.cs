using Application.Attribute.Features.Shared;

namespace Application.Attribute.Features.Queries.GetAllAttributeTypes;

public record GetAllAttributeTypesQuery(
    int Page = 1,
    int PageSize = 10) : IPageQuery<AttributeTypeDto>;