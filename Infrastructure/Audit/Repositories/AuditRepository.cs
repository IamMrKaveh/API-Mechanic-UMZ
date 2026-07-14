using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Domain.Audit.ValueObjects;

namespace Infrastructure.Audit.Repositories;

public sealed class AuditRepository(DBContext context) : IAuditRepository
{
    public async Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken ct = default)
    {
        await context.AuditLogs.AddAsync(auditLog, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<AuditLog?> GetByIdAsync(
        AuditLogId id,
        CancellationToken ct = default) =>
        await context.AuditLogs
            .FirstOrDefaultAsync(l => l.Id == id, ct);
}