using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProduct;

public record GetProductQuery(Guid Id) : IRequest<ServiceResult<ProductDetailDto>>;