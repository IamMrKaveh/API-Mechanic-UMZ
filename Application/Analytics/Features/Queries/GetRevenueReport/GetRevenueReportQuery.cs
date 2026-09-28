using Application.Analytics.Constants;
using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed record GetRevenueReportQuery(
    DateTime FromDate,
    DateTime ToDate) : IQuery<RevenueReportDto>, ICacheableQuery
{
    public string CacheKey => AnalyticsCacheKeys.RevenueReport(FromDate, ToDate);

    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}