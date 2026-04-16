using Domain.Media.Interfaces;
using Domain.Media.ValueObjects;

namespace Infrastructure.Media.Repositories;

public sealed class MediaRepository(DBContext context) : IMediaRepository
{
    public async Task AddAsync(
        Domain.Media.Aggregates.Media media,
        CancellationToken ct = default)
    {
        await context.Medias.AddAsync(media, ct);
    }

    public void Update(Domain.Media.Aggregates.Media media)
    {
        context.Medias.Update(media);
    }

    public void Remove(Domain.Media.Aggregates.Media media)
    {
        context.Medias.Remove(media);
    }

    public async Task<Domain.Media.Aggregates.Media?> GetByIdAsync(
        MediaId id,
        CancellationToken ct = default)
    {
        return await context.Medias
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<IReadOnlyList<Domain.Media.Aggregates.Media>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default)
    {
        var results = await context.Medias
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<Domain.Media.Aggregates.Media?> GetPrimaryByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default)
    {
        return await context.Medias
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && m.IsPrimary)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlySet<string>> GetAllFilePathsAsync(
        CancellationToken ct = default)
    {
        var paths = await context.Medias
            .IgnoreQueryFilters()
            .Select(m => m.Path.Value)
            .ToListAsync(ct);

        return paths.ToHashSet();
    }
}