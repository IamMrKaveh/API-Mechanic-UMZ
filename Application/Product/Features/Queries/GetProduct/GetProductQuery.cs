using Application.Cache.Features.Shared;
using Application.Cache.Interfaces;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProduct;

public record GetProductQuery(Guid Id)
    : IRequest<ServiceResult<ProductDetailDto>>, ICacheableQuery, IQuery
{
    public string CacheKey => CacheKeys.Product(Id);
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}