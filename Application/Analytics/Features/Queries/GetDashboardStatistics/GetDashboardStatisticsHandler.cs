namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed class GetDashboardStatisticsHandler
    : IRequestHandler<GetDashboardStatisticsQuery, ServiceResult<DashboardStatisticsDto>>
{
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly ICacheService _cache;
    private readonly ILogger<GetDashboardStatisticsHandler> _logger;

    public GetDashboardStatisticsHandler(
        IAnalyticsQueryService analyticsQuery,
        ICacheService cache,
        ILogger<GetDashboardStatisticsHandler> logger
        )
    {
        _analyticsQuery = analyticsQuery;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ServiceResult<DashboardStatisticsDto>> Handle(
        GetDashboardStatisticsQuery request,
        CancellationToken cancellationToken
        )
    {
        var cacheKey = $"analytics:dashboard:{request.FromDate?.ToString("yyyyMMdd")}:{request.ToDate?.ToString("yyyyMMdd")}";

        var cached = await _cache.GetAsync<DashboardStatisticsDto>(cacheKey);
        if (cached is not null)
            return ServiceResult<DashboardStatisticsDto>.Success(cached);

        var result = await _analyticsQuery.GetDashboardStatisticsAsync(
            request.FromDate, request.ToDate, cancellationToken);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        return ServiceResult<DashboardStatisticsDto>.Success(result);
    }
}