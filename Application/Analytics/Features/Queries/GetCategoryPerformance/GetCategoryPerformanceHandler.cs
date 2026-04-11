using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed class GetCategoryPerformanceHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetCategoryPerformanceQuery, ServiceResult<PaginatedResult<CategoryPerformanceDto>>>
{
    public async Task<ServiceResult<PaginatedResult<CategoryPerformanceDto>>> Handle(
        GetCategoryPerformanceQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:category-perf:{request.FromDate?.ToString("yyyyMMdd")}:{request.ToDate?.ToString("yyyyMMdd")}";

        var cached = await cache.GetAsync<PaginatedResult<CategoryPerformanceDto>>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<PaginatedResult<CategoryPerformanceDto>>.Success(cached);

        var result = await analyticsQuery.GetCategoryPerformanceAsync(
            request.FromDate, request.ToDate, ct);

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15), ct);

        return ServiceResult<PaginatedResult<CategoryPerformanceDto>>.Success(result);
    }
}