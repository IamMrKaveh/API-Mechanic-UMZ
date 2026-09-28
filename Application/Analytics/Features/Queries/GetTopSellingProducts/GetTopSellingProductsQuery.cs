using Application.Analytics.Constants;
using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

public sealed record GetTopSellingProductsQuery(
    int Count = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 10) : IPageQuery<TopSellingProductDto>, ICacheableQuery
{
    public string CacheKey => AnalyticsCacheKeys.TopSellingProducts(Count, FromDate, ToDate);

    public TimeSpan? Expiry => TimeSpan.FromMinutes(15);
}