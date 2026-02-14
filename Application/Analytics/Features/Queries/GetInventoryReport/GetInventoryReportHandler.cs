namespace Application.Analytics.Features.Queries.GetInventoryReport;

public sealed class GetInventoryReportHandler
    : IRequestHandler<GetInventoryReportQuery, ServiceResult<InventoryReportDto>>
{
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly ICacheService _cache;

    public GetInventoryReportHandler(
        IAnalyticsQueryService analyticsQuery,
        ICacheService cache)
    {
        _analyticsQuery = analyticsQuery;
        _cache = cache;
    }

    public async Task<ServiceResult<InventoryReportDto>> Handle(
        GetInventoryReportQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "analytics:inventory-report";

        var cached = await _cache.GetAsync<InventoryReportDto>(cacheKey);
        if (cached is not null)
            return ServiceResult<InventoryReportDto>.Success(cached);

        var result = await _analyticsQuery.GetInventoryReportAsync(cancellationToken);

        //await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

        return ServiceResult<InventoryReportDto>.Success(result);
    }
}