namespace Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(int Id) : IRequest<ServiceResult<PublicProductViewDto?>>;