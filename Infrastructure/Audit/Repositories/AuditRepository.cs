using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Audit.Repositories;

public sealed class AuditRepository(DBContext context) : IAuditRepository
{
    public async Task AddAuditLogAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        await context.AuditLogs.AddAsync(auditLog, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
    {
        return await context.AuditLogs
            .AsNoTracking()
            .Where(l => l.EventType == entityType && l.Details.Contains(entityId))
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        return await context.AuditLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
    }
}