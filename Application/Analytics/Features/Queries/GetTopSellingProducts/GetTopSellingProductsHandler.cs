using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

public sealed class GetTopSellingProductsHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetTopSellingProductsQuery, ServiceResult<PaginatedResult<TopSellingProductDto>>>
{
    public async Task<ServiceResult<PaginatedResult<TopSellingProductDto>>> Handle(
        GetTopSellingProductsQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:top-products:{request.Count}:{request.FromDate?.ToString("yyyyMMdd")}:{request.ToDate?.ToString("yyyyMMdd")}";

        var cached = await cache.GetAsync<PaginatedResult<TopSellingProductDto>>(cacheKey);
        if (cached is not null)
            return ServiceResult<PaginatedResult<TopSellingProductDto>>.Success(cached);

        var result = await analyticsQuery.GetTopSellingProductsAsync(
            request.Count, request.FromDate, request.ToDate, ct);

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), ct);

        return ServiceResult<PaginatedResult<TopSellingProductDto>>.Success(result);
    }
}