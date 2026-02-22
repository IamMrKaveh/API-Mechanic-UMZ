namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed class GetCategoryPerformanceHandler
    : IRequestHandler<GetCategoryPerformanceQuery, ServiceResult<IReadOnlyList<CategoryPerformanceDto>>>
{
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly ICacheService _cache;

    public GetCategoryPerformanceHandler(
        IAnalyticsQueryService analyticsQuery,
        ICacheService cache
        )
    {
        _analyticsQuery = analyticsQuery;
        _cache = cache;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryPerformanceDto>>> Handle(
        GetCategoryPerformanceQuery request,
        CancellationToken cancellationToken
        )
    {
        var cacheKey = $"analytics:category-perf:{request.FromDate?.ToString("yyyyMMdd")}:{request.ToDate?.ToString("yyyyMMdd")}";

        var cached = await _cache.GetAsync<IReadOnlyList<CategoryPerformanceDto>>(cacheKey);
        if (cached is not null)
            return ServiceResult<IReadOnlyList<CategoryPerformanceDto>>.Success(cached);

        var result = await _analyticsQuery.GetCategoryPerformanceAsync(
            request.FromDate, request.ToDate, cancellationToken);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        return ServiceResult<IReadOnlyList<CategoryPerformanceDto>>.Success(result);
    }
}