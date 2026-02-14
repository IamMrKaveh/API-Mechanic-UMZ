namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed record GetRevenueReportQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<ServiceResult<RevenueReportDto>>;