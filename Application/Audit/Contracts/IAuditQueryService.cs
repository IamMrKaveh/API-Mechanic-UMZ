using Application.Audit.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Audit.Contracts;

public interface IAuditQueryService
{
    Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(
        UserId? userId,
        string? eventType,
        string? entityType,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditLogDto>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLogDto> Logs, int Total)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default);

    Task<byte[]> ExportToCsvAsync(
        AuditExportRequest request,
        CancellationToken ct = default);
}