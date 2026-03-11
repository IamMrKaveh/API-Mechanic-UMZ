using Domain.Media.Interfaces;

namespace Infrastructure.Media.Repositories;

public class MediaRepository(DBContext context) : IMediaRepository
{
    private readonly DBContext _context = context;

    public async Task AddAsync(
        Domain.Media.Aggregates.Media media,
        CancellationToken ct = default)
    {
        await _context.Medias.AddAsync(media, ct);
    }

    public void Update(Domain.Media.Aggregates.Media media)
    {
        _context.Medias.Update(media);
    }

    public void Remove(Domain.Media.Aggregates.Media media)
    {
        _context.Medias.Remove(media);
    }
}