namespace Domain.Media.Interfaces;

public interface IMediaRepository
{
    Task AddAsync(Aggregates.Media media, CancellationToken ct = default);

    void Update(Aggregates.Media media);

    void Remove(Aggregates.Media media);

    Task<Aggregates.Media?> GetByIdAsync(MediaId id, CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Media>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    Task<Aggregates.Media?> GetPrimaryByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    Task<IReadOnlySet<string>> GetAllFilePathsAsync(CancellationToken ct = default);
}