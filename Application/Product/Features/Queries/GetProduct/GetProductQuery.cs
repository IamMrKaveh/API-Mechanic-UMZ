using Application.Common.Results;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProduct;

public record GetProductQuery(int Id) : IRequest<ServiceResult<ProductDetailDto>>;