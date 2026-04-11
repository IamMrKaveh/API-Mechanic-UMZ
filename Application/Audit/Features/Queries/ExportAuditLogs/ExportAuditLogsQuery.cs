using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.ExportAuditLogs;

public sealed record ExportAuditLogsQuery(
    Guid? UserId,
    string? EventType,
    string? EntityType,
    DateTime? From,
    DateTime? To,
    string Format = "csv",
    int MaxRows = 10_000) : IRequest<ServiceResult<PaginatedResult<ExportAuditLogsResult>>>;