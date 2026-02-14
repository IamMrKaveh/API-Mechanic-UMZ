namespace Application.Product.Features.Queries.GetProductById;

public record GetProductByIdQuery(int Id) : IRequest<ServiceResult<PublicProductDetailDto?>>;