namespace Application.Audit.Features.Queries.ExportAuditLogs;

public sealed record ExportAuditLogsQuery(
    int? UserId,
    string? EventType,
    DateTime? From,
    DateTime? To,
    string Format = "csv",
    int MaxRows = 10_000
) : IRequest<ExportAuditLogsResult>;

public sealed record ExportAuditLogsResult(
    byte[] FileContent,
    string ContentType,
    string FileName);