using Domain.Audit.Entities;
using Domain.Audit.Interfaces;

namespace Infrastructure.Audit.Repositories;

public sealed class AuditRepository(DBContext context) : IAuditRepository
{
    public async Task AddAuditLogAsync(AuditLog auditLog, CancellationToken ct = default)
    {
        await context.AuditLogs.AddAsync(auditLog, ct);
        await context.SaveChangesAsync(ct);
    }
}