using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using Application.Common.Results;

namespace Application.Analytics.Features.Queries.GetInventoryReport;

public sealed class GetInventoryReportHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache)
        : IRequestHandler<GetInventoryReportQuery, ServiceResult<InventoryReportDto>>
{
    private readonly IAnalyticsQueryService _analyticsQuery = analyticsQuery;
    private readonly ICacheService _cache = cache;

    public async Task<ServiceResult<InventoryReportDto>> Handle(
        GetInventoryReportQuery request,
        CancellationToken ct)
    {
        const string cacheKey = "analytics:inventory-report";

        var cached = await _cache.GetAsync<InventoryReportDto>(cacheKey);
        if (cached is not null)
            return ServiceResult<InventoryReportDto>.Success(cached);

        var result = await _analyticsQuery.GetInventoryReportAsync(ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return ServiceResult<InventoryReportDto>.Success(result);
    }
}