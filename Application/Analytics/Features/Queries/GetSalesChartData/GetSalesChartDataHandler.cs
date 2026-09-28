using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed class GetSalesChartDataHandler(
    IAnalyticsQueryService analyticsQuery)
    : IQueryHandler<GetSalesChartDataQuery, PaginatedResult<SalesChartDataPointDto>>
{
    public async Task<ServiceResult<PaginatedResult<SalesChartDataPointDto>>> Handle(
        GetSalesChartDataQuery request,
        CancellationToken ct)
    {
        var result = await analyticsQuery.GetSalesChartDataAsync(
            request.FromDate, request.ToDate, request.GroupBy, ct);

        return ServiceResult<PaginatedResult<SalesChartDataPointDto>>.Success(result);
    }
}