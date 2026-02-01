namespace Infrastructure.Persistence.Repositories.Media;

public class MediaRepository : IMediaRepository
{
    private readonly LedkaContext _context;

    public MediaRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Domain.Media.Media?> GetByIdAsync(int id)
    {
        return await _context.Medias.FindAsync(id);
    }

    public async Task<IEnumerable<Domain.Media.Media>> GetByEntityAsync(string entityType, int entityId)
    {
        return await _context.Medias
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();
    }

    public async Task<Domain.Media.Media?> GetPrimaryMediaByEntityAsync(string entityType, int entityId)
    {
        return await _context.Medias
            .Where(m => m.EntityType == entityType && m.EntityId == entityId && m.IsPrimary)
            .FirstOrDefaultAsync();
    }


    public async Task AddAsync(Domain.Media.Media media)
    {
        await _context.Medias.AddAsync(media);
    }

    public void Update(Domain.Media.Media media)
    {
        _context.Entry(media).State = EntityState.Modified;
    }


    public void Remove(Domain.Media.Media media)
    {
        _context.Medias.Remove(media);
    }

    public void SetOriginalRowVersion(Domain.Media.Media entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
    }
}