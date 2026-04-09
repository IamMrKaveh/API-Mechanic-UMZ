using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed class GetSalesChartDataHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetSalesChartDataQuery, ServiceResult<PaginatedResult<SalesChartDataPointDto>>>
{
    private readonly IAnalyticsQueryService _analyticsQuery = analyticsQuery;
    private readonly ICacheService _cache = cache;

    public async Task<ServiceResult<PaginatedResult<SalesChartDataPointDto>>> Handle(
        GetSalesChartDataQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:sales-chart:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}:{request.GroupBy}";

        var cached = await _cache.GetAsync<PaginatedResult<SalesChartDataPointDto>>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<PaginatedResult<SalesChartDataPointDto>>.Success(cached);

        var result = await _analyticsQuery.GetSalesChartDataAsync(
            request.FromDate, request.ToDate, request.GroupBy, ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        return ServiceResult<PaginatedResult<SalesChartDataPointDto>>.Success(result);
    }
}