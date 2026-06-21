using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditStatistics;

public sealed record GetAuditStatisticsQuery(
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 10) : IPageQuery<AuditStatisticsDto>;