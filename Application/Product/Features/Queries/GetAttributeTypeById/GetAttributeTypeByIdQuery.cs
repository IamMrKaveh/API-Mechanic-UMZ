namespace Application.Product.Features.Queries.GetAttributeTypeById;

public record GetAttributeTypeByIdQuery(int Id) : IRequest<ServiceResult<AttributeTypeDto?>>;