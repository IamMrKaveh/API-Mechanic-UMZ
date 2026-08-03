using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.ExportAuditLogs;

public sealed record ExportAuditLogsQuery(
    Guid? UserId,
    string? EventType,
    string? EntityType,
    DateTime? From,
    DateTime? To,
    string Format,
    int? MaxRows) : IQuery<ExportAuditLogsResult>;
