using Domain.Audit.Entities;

namespace Domain.Audit.Interfaces;

public interface IAuditRepository
{
    Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken ct = default);
}