namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed record GetDashboardStatisticsQuery(
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<ServiceResult<DashboardStatisticsDto>>;