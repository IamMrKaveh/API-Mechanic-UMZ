using Application.Analytics.Constants;
using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed record GetDashboardStatisticsQuery(
    DateTime? FromDate,
    DateTime? ToDate) : IQuery<DashboardStatisticsDto>, ICacheableQuery
{
    public string CacheKey => AnalyticsCacheKeys.Dashboard(FromDate, ToDate);

    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}