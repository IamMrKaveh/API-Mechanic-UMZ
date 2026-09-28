using Application.Analytics.Constants;
using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetInventoryReport;

public sealed record GetInventoryReportQuery : IQuery<InventoryReportDto>, ICacheableQuery
{
    public string CacheKey => AnalyticsCacheKeys.InventoryReport;

    public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
}