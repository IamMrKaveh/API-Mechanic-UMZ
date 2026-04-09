using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    Guid? UserId,
    string? EventType,
    string? Action,
    string? Keyword,
    string? IpAddress,
    DateTime? From,
    DateTime? To,
    int Page = 1,
    int PageSize = 50,
    string SortBy = "Timestamp",
    bool SortDesc = true
) : IRequest<ServiceResult<PaginatedResult<GetAuditLogsResult>>>;