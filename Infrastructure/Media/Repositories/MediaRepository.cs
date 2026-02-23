namespace Infrastructure.Media.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly Persistence.Context.DBContext _context;

    public MediaRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<Domain.Media.Media?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Medias
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
    }

    public async Task<IReadOnlyList<Domain.Media.Media>> GetByEntityAsync(
        string entityType, int entityId, CancellationToken ct = default)
    {
        return await _context.Medias
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Domain.Media.Media?> GetPrimaryByEntityAsync(
        string entityType, int entityId, CancellationToken ct = default)
    {
        return await _context.Medias
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && m.IsPrimary
                        && !m.IsDeleted)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountByEntityAsync(
        string entityType, int entityId, CancellationToken ct = default)
    {
        return await _context.Medias
            .CountAsync(m => m.EntityType == entityType
                             && m.EntityId == entityId
                             && !m.IsDeleted, ct);
    }

    public async Task<IReadOnlySet<string>> GetAllFilePathsAsync(CancellationToken ct = default)
    {
        var paths = await _context.Medias
            .IgnoreQueryFilters()
            .Select(m => m.FilePath)
            .ToListAsync(ct);

        return paths.ToHashSet();
    }

    public async Task AddAsync(Domain.Media.Media media, CancellationToken ct = default)
    {
        await _context.Medias.AddAsync(media, ct);
    }

    public void Update(Domain.Media.Media media)
    {
        _context.Medias.Update(media);
    }

    public void Remove(Domain.Media.Media media)
    {
        _context.Medias.Remove(media);
    }
}