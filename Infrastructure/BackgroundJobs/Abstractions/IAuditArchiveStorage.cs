using Domain.Audit.Entities;

namespace Infrastructure.BackgroundJobs.Abstractions;

public interface IAuditArchiveStorage
{
    Task ArchiveAsync(
        IEnumerable<AuditLog> logs,
        string label,
        DateTime timestamp,
        CancellationToken ct);
}