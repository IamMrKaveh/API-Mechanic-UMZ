namespace Domain.Audit.Interfaces;

public interface IAuditRepository
{
    Task AddAuditLogAsync(
        AuditLog auditLog,
        CancellationToken ct = default);

    Task<IEnumerable<AuditLog>> GetByUserIdAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IEnumerable<AuditLog>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);
}