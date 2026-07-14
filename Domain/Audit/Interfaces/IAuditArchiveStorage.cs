using Domain.Audit.Entities;

namespace Domain.Audit.Interfaces;

public interface IAuditArchiveStorage
{
    Task ArchiveAsync(IEnumerable<AuditLog> logs, CancellationToken ct = default);

    Task<Stream?> ReadArchiveAsync(DateOnly date, CancellationToken ct = default);
}