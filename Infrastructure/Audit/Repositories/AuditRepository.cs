using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Domain.Audit.ValueObjects;

namespace Infrastructure.Audit.Repositories;

public sealed class AuditRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<AuditLog, AuditLogId>(context), IAuditRepository
{
    public async Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken ct = default)
    {
        await AddAsync(auditLog, ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetForArchiveAsync(
        DateTime cutoff,
        HashSet<string>? includeEventTypes,
        HashSet<string>? excludeEventTypes,
        bool onlyNonArchived,
        int batchSize,
        CancellationToken ct = default)
    {
        var query = Context.AuditLogs.Where(a => a.CreatedAt < cutoff);

        if (onlyNonArchived)
            query = query.Where(a => !a.IsArchived);

        if (includeEventTypes is { Count: > 0 })
            query = query.Where(a => includeEventTypes.Contains(a.EventType));

        if (excludeEventTypes is { Count: > 0 })
            query = query.Where(a => !excludeEventTypes.Contains(a.EventType));

        var logs = await query
            .OrderBy(a => a.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        return logs.AsReadOnly();
    }

    public Task RemoveRangeAsync(
        IEnumerable<AuditLog> logs,
        CancellationToken ct = default)
    {
        Context.AuditLogs.RemoveRange(logs);
        return Task.CompletedTask;
    }
}
