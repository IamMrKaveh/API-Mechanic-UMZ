using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetInventoryReport;

public sealed class GetInventoryReportHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache)
        : IRequestHandler<GetInventoryReportQuery, ServiceResult<InventoryReportDto>>
{
    public async Task<ServiceResult<InventoryReportDto>> Handle(
        GetInventoryReportQuery request,
        CancellationToken ct)
    {
        const string cacheKey = "analytics:inventory-report";

        var cached = await cache.GetAsync<InventoryReportDto>(cacheKey);
        if (cached is not null)
            return ServiceResult<InventoryReportDto>.Success(cached);

        var result = await analyticsQuery.GetInventoryReportAsync(ct);

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);

        return ServiceResult<InventoryReportDto>.Success(result);
    }
}