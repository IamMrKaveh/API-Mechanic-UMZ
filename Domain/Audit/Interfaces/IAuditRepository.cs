using Domain.Audit.Entities;
using Domain.User.ValueObjects;

namespace Domain.Audit.Interfaces;

public interface IAuditRepository
{
    Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken ct = default);

    Task<IEnumerable<AuditLog>> GetByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);

    Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default);
}