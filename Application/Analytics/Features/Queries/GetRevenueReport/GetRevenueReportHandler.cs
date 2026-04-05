using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using Application.Common.Results;

namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed class GetRevenueReportHandler(
    IAnalyticsQueryService analyticsQuery,
    ICacheService cache) : IRequestHandler<GetRevenueReportQuery, ServiceResult<RevenueReportDto>>
{
    private readonly IAnalyticsQueryService _analyticsQuery = analyticsQuery;
    private readonly ICacheService _cache = cache;

    public async Task<ServiceResult<RevenueReportDto>> Handle(
        GetRevenueReportQuery request,
        CancellationToken cancellationToken
        )
    {
        var cacheKey = $"analytics:revenue:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}";

        var cached = await _cache.GetAsync<RevenueReportDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return ServiceResult<RevenueReportDto>.Success(cached);

        var result = await _analyticsQuery.GetRevenueReportAsync(
            request.FromDate, request.ToDate, cancellationToken);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));

        return ServiceResult<RevenueReportDto>.Success(result);
    }
}