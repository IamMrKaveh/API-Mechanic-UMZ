using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed class GetRevenueReportHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetRevenueReportQuery, ServiceResult<RevenueReportDto>>
{
    public async Task<ServiceResult<RevenueReportDto>> Handle(
        GetRevenueReportQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"analytics:revenue:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}";

        var cached = await cache.GetAsync<RevenueReportDto>(cacheKey, ct);
        if (cached is not null)
            return ServiceResult<RevenueReportDto>.Success(cached);

        var result = await analyticsQuery.GetRevenueReportAsync(
            request.FromDate, request.ToDate, ct);

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10), ct);

        return ServiceResult<RevenueReportDto>.Success(result);
    }
}