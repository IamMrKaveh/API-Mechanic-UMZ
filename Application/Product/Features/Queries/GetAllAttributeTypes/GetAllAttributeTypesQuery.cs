namespace Application.Product.Features.Queries.GetAllAttributeTypes;

public record GetAllAttributeTypesQuery : IRequest<ServiceResult<IEnumerable<AttributeTypeDto>>>;