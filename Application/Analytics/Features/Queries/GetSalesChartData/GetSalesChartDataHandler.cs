namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed class GetSalesChartDataHandler
    : IRequestHandler<GetSalesChartDataQuery, ServiceResult<IReadOnlyList<SalesChartDataPointDto>>>
{
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly ICacheService _cache;

    public GetSalesChartDataHandler(
        IAnalyticsQueryService analyticsQuery,
        ICacheService cache
        )
    {
        _analyticsQuery = analyticsQuery;
        _cache = cache;
    }

    public async Task<ServiceResult<IReadOnlyList<SalesChartDataPointDto>>> Handle(
        GetSalesChartDataQuery request,
        CancellationToken cancellationToken
        )
    {
        var cacheKey = $"analytics:sales-chart:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}:{request.GroupBy}";

        var cached = await _cache.GetAsync<IReadOnlyList<SalesChartDataPointDto>>(cacheKey);
        if (cached is not null)
            return ServiceResult<IReadOnlyList<SalesChartDataPointDto>>.Success(cached);

        var result = await _analyticsQuery.GetSalesChartDataAsync(
            request.FromDate, request.ToDate, request.GroupBy, cancellationToken);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));

        return ServiceResult<IReadOnlyList<SalesChartDataPointDto>>.Success(result);
    }
}