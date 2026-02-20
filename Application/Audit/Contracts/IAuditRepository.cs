namespace Application.Audit.Contracts;

public interface IAuditRepository
{
    Task AddAuditLogAsync(AuditLog log);

    Task<(IEnumerable<AuditLog> Logs, int Total)> GetAuditLogsAsync(DateTime? from, DateTime? to, int? userId, string? type, int page, int size);

    Task<(IEnumerable<AuditDtos> Logs, int Total)> SearchAsync(AuditSearchRequest request, CancellationToken ct = default);
}