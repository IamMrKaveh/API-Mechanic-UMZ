namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    int? UserId,
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
) : IRequest<GetAuditLogsResult>;

public sealed record GetAuditLogsResult(
    IEnumerable<AuditDtos> Items,
    int Total,
    int Page,
    int PageSize,
    int TotalPages);