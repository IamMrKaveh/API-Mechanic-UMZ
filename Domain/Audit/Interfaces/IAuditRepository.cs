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
}