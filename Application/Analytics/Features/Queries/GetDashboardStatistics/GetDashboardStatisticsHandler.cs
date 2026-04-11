using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed class GetDashboardStatisticsHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetDashboardStatisticsQuery, ServiceResult<DashboardStatisticsDto>>
{
    public async Task<ServiceResult<DashboardStatisticsDto>> Handle(
        GetDashboardStatisticsQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:dashboard:{request.FromDate?.ToString("yyyyMMdd")}:{request.ToDate?.ToString("yyyyMMdd")}";

        var cached = await cache.GetAsync<DashboardStatisticsDto>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<DashboardStatisticsDto>.Success(cached);

        var result = await analyticsQuery.GetDashboardStatisticsAsync(
            request.FromDate, request.ToDate, ct);

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), ct);

        return ServiceResult<DashboardStatisticsDto>.Success(result);
    }
}