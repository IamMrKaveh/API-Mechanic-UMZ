using Application.Audit.Features.Shared;

namespace Application.Audit.Contracts;

public interface IAuditQueryService
{
    Task<(IEnumerable<AuditDtos> Logs, int Total)> GetAuditLogsAsync(
        DateTime? from,
        DateTime? to,
        int? userId,
        string? type,
        int page,
        int size,
        CancellationToken ct = default);

    Task<(IEnumerable<AuditDtos> Logs, int Total)> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default);
}