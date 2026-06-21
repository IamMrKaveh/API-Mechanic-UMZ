using Application.Cache.Features.Shared;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProduct;

public record GetProductQuery(Guid Id)
    : IQuery<ProductDetailDto>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Product(Id);

    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}