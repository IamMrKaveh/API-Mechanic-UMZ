using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed class GetRevenueReportHandler(
    IAnalyticsQueryService analyticsQuery)
    : IQueryHandler<GetRevenueReportQuery, RevenueReportDto>
{
    public async Task<ServiceResult<RevenueReportDto>> Handle(
        GetRevenueReportQuery request,
        CancellationToken ct)
    {
        var result = await analyticsQuery.GetRevenueReportAsync(
            request.FromDate, request.ToDate, ct);

        return ServiceResult<RevenueReportDto>.Success(result);
    }
}