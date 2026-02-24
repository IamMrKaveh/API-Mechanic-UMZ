namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed record GetSalesChartDataQuery(
    DateTime FromDate,
    DateTime ToDate,
    string GroupBy = "day"
    ) : IRequest<ServiceResult<IReadOnlyList<SalesChartDataPointDto>>>;