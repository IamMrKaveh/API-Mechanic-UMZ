namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed class GetRevenueReportHandler
    : IRequestHandler<GetRevenueReportQuery, ServiceResult<RevenueReportDto>>
{
    private readonly IAnalyticsQueryService _analyticsQuery;
    private readonly ICacheService _cache;

    public GetRevenueReportHandler(
        IAnalyticsQueryService analyticsQuery,
        ICacheService cache
        )
    {
        _analyticsQuery = analyticsQuery;
        _cache = cache;
    }

    public async Task<ServiceResult<RevenueReportDto>> Handle(
        GetRevenueReportQuery request,
        CancellationToken cancellationToken
        )
    {
        var cacheKey = $"analytics:revenue:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}";

        var cached = await _cache.GetAsync<RevenueReportDto>(cacheKey);
        if (cached is not null)
            return ServiceResult<RevenueReportDto>.Success(cached);

        var result = await _analyticsQuery.GetRevenueReportAsync(
            request.FromDate, request.ToDate, cancellationToken);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        return ServiceResult<RevenueReportDto>.Success(result);
    }
}