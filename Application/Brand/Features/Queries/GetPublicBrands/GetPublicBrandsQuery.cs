using Application.Brand.Features.Shared;

namespace Application.Brand.Features.Queries.GetPublicBrands;

public sealed record GetPublicBrandsQuery(Guid? CategoryId) : IQuery<IReadOnlyList<BrandListItemDto>>, ICacheableQuery
{
    public string CacheKey => $"brands:public:category={CategoryId?.ToString() ?? "all"}";

    public TimeSpan? Expiry => TimeSpan.FromMinutes(30);
}