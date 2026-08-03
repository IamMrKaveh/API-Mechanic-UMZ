using Domain.Audit.Entities;
using Domain.Audit.ValueObjects;

namespace Domain.Audit.Interfaces;

public interface IAuditRepository
{
    Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken ct = default);

    Task<AuditLog?> GetByIdAsync(
        AuditLogId id,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditLog>> GetForArchiveAsync(
        DateTime cutoff,
        HashSet<string>? includeEventTypes,
        HashSet<string>? excludeEventTypes,
        bool onlyNonArchived,
        int batchSize,
        CancellationToken ct = default);

    Task RemoveRangeAsync(
        IEnumerable<AuditLog> logs,
        CancellationToken ct = default);
}
