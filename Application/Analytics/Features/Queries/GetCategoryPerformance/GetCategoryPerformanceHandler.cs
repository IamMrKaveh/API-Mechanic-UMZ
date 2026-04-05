using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using Application.Common.Results;

namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed class GetCategoryPerformanceHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetCategoryPerformanceQuery, ServiceResult<IReadOnlyList<CategoryPerformanceDto>>>
{
    private readonly IAnalyticsQueryService _analyticsQuery = analyticsQuery;
    private readonly ICacheService _cache = cache;

    public async Task<ServiceResult<IReadOnlyList<CategoryPerformanceDto>>> Handle(
        GetCategoryPerformanceQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:category-perf:{request.FromDate?.ToString("yyyyMMdd")}:{request.ToDate?.ToString("yyyyMMdd")}";

        var cached = await _cache.GetAsync<IReadOnlyList<CategoryPerformanceDto>>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<IReadOnlyList<CategoryPerformanceDto>>.Success(cached);

        var result = await _analyticsQuery.GetCategoryPerformanceAsync(
            request.FromDate, request.ToDate, ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        return ServiceResult<IReadOnlyList<CategoryPerformanceDto>>.Success(result);
    }
}