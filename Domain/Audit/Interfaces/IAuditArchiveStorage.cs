using Domain.Audit.Entities;

namespace Domain.Audit.Interfaces;

public interface IAuditArchiveStorage
{
    Task ArchiveAsync(
        IEnumerable<AuditLog> logs,
        string label,
        DateTime timestamp,
        CancellationToken ct);
}