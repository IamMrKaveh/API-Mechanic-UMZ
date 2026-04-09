using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed record GetDashboardStatisticsQuery(
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<ServiceResult<PaginatedResult<DashboardStatisticsDto>>>;