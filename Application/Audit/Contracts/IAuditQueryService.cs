using Application.Audit.Features.Shared;

namespace Application.Audit.Contracts;

public interface IAuditQueryService
{
    Task<PaginatedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid? userId,
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
}