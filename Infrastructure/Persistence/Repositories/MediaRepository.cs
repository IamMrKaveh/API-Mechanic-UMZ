using Domain.Media;

namespace Infrastructure.Persistence.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly LedkaContext _context;

    public MediaRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Media?> GetByIdAsync(int id)
    {
        return await _context.Medias.FindAsync(id);
    }

    public async Task<IEnumerable<Media>> GetByEntityAsync(string entityType, int entityId)
    {
        return await _context.Medias
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();
    }

    public async Task<Media?> GetPrimaryMediaByEntityAsync(string entityType, int entityId)
    {
        return await _context.Medias
            .Where(m => m.EntityType == entityType && m.EntityId == entityId && m.IsPrimary)
            .FirstOrDefaultAsync();
    }


    public async Task AddAsync(Media media)
    {
        await _context.Medias.AddAsync(media);
    }

    public void Remove(Media media)
    {
        _context.Medias.Remove(media);
    }

    public void SetOriginalRowVersion(Media entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
    }
}