using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed class GetDashboardStatisticsHandler(
    IAnalyticsQueryService analyticsQuery)
    : IQueryHandler<GetDashboardStatisticsQuery, DashboardStatisticsDto>
{
    public async Task<ServiceResult<DashboardStatisticsDto>> Handle(
        GetDashboardStatisticsQuery request,
        CancellationToken ct)
    {
        var result = await analyticsQuery.GetDashboardStatisticsAsync(
            request.FromDate, request.ToDate, ct);

        return ServiceResult<DashboardStatisticsDto>.Success(result);
    }
}
