namespace Application.Common.Interfaces.Persistence;

public interface IAuditRepository
{
    Task AddAuditLogAsync(Domain.Log.AuditLog auditLog);

    Task<(IEnumerable<Domain.Log.AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? userId = null,
        string? eventType = null,
        int page = 1,
        int pageSize = 50);
}