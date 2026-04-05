using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using Application.Common.Results;

namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed class GetSalesChartDataHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetSalesChartDataQuery, ServiceResult<IReadOnlyList<SalesChartDataPointDto>>>
{
    private readonly IAnalyticsQueryService _analyticsQuery = analyticsQuery;
    private readonly ICacheService _cache = cache;

    public async Task<ServiceResult<IReadOnlyList<SalesChartDataPointDto>>> Handle(
        GetSalesChartDataQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:sales-chart:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}:{request.GroupBy}";

        var cached = await _cache.GetAsync<IReadOnlyList<SalesChartDataPointDto>>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<IReadOnlyList<SalesChartDataPointDto>>.Success(cached);

        var result = await _analyticsQuery.GetSalesChartDataAsync(
            request.FromDate, request.ToDate, request.GroupBy, ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        return ServiceResult<IReadOnlyList<SalesChartDataPointDto>>.Success(result);
    }
}