using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditStatistics;

public sealed record GetAuditStatisticsQuery(
    DateTime? From,
    DateTime? To) : IRequest<ServiceResult<PaginatedResult<AuditStatisticsDto>>>;