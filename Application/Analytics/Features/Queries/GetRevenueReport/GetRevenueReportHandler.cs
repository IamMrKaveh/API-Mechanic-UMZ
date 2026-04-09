using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed class GetRevenueReportHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetRevenueReportQuery, ServiceResult<RevenueReportDto>>
{
    private readonly IAnalyticsQueryService _analyticsQuery = analyticsQuery;
    private readonly ICacheService _cache = cache;

    public async Task<ServiceResult<RevenueReportDto>> Handle(
        GetRevenueReportQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:revenue:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}";

        var cached = await _cache.GetAsync<RevenueReportDto>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<RevenueReportDto>.Success(cached);

        var result = await _analyticsQuery.GetRevenueReportAsync(
            request.FromDate, request.ToDate, ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        return ServiceResult<RevenueReportDto>.Success(result);
    }
}