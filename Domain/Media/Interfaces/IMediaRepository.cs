using Domain.Common.Interfaces;
using Domain.Media.ValueObjects;

namespace Domain.Media.Interfaces;

public interface IMediaRepository : IRepository<Aggregates.Media, MediaId>
{
    Task<IReadOnlyList<Aggregates.Media>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    Task<Aggregates.Media?> GetPrimaryByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Media>> GetByPathAsync(
        string filePath,
        CancellationToken ct = default);

    Task<IReadOnlySet<string>> GetAllFilePathsAsync(CancellationToken ct = default);
}