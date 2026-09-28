using Domain.Media.Interfaces;
using Domain.Media.ValueObjects;

using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Media.Repositories;

public sealed class MediaRepository(DBContext context)
    : RepositoryBase<Domain.Media.Aggregates.Media, MediaId>(context), IMediaRepository
{
    public async Task<IReadOnlyList<Domain.Media.Aggregates.Media>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var results = await Context.Medias
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<Domain.Media.Aggregates.Media?> GetPrimaryByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        return await Context.Medias
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && m.IsPrimary)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Media.Aggregates.Media>> GetByPathAsync(
        string filePath,
        CancellationToken ct = default)
    {
        var results = await Context.Medias
            .Where(m => m.Path.Value == filePath)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlySet<string>> GetAllFilePathsAsync(
        CancellationToken ct = default)
    {
        var paths = await Context.Medias
            .IgnoreQueryFilters()
            .Select(m => m.Path.Value)
            .ToListAsync(ct);

        return paths.ToHashSet();
    }
}