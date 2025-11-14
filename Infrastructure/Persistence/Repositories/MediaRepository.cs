namespace Infrastructure.Persistence.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly LedkaContext _context;

    public MediaRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task AddMediaAsync(Domain.Media.Media media)
    {
        await _context.Set<Domain.Media.Media>().AddAsync(media);
    }

    public async Task<Domain.Media.Media?> GetMediaByIdAsync(int mediaId)
    {
        return await _context.Set<Domain.Media.Media>().FindAsync(mediaId);
    }

    public async Task<IEnumerable<Domain.Media.Media>> GetMediaForEntityAsync(string entityType, int entityId)
    {
        return await _context.Set<Domain.Media.Media>()
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderBy(m => m.SortOrder)
            .ThenByDescending(m => m.IsPrimary)
            .ToListAsync();
    }

    public async Task<string?> GetPrimaryMediaFilePathAsync(string entityType, int entityId)
    {
        return await _context.Set<Domain.Media.Media>()
            .Where(m => m.EntityType == entityType && m.EntityId == entityId && m.IsPrimary)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync();
    }

    public void DeleteMedia(Domain.Media.Media media)
    {
        _context.Set<Domain.Media.Media>().Remove(media);
    }

    public async Task UnsetPrimaryMediaAsync(string entityType, int entityId, int? excludeMediaId = null)
    {
        var query = _context.Set<Domain.Media.Media>()
            .Where(m => m.EntityType == entityType && m.EntityId == entityId && m.IsPrimary);

        if (excludeMediaId.HasValue)
        {
            query = query.Where(m => m.Id != excludeMediaId.Value);
        }

        await query.ExecuteUpdateAsync(s => s.SetProperty(m => m.IsPrimary, false));
    }
}