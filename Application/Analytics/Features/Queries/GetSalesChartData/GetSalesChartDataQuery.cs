using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed record GetSalesChartDataQuery(
    DateTime FromDate,
    DateTime ToDate,
    string GroupBy = "day",
    int Page = 1,
    int PageSize = 10) : IPageQuery<SalesChartDataPointDto>;