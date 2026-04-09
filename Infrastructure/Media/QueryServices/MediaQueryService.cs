using Application.Media.Contracts;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Media.QueryServices;

public class MediaQueryService(DBContext context, IStorageService storageService) : IMediaQueryService
{
    private readonly DBContext _context = context;
    private readonly IStorageService _storageService = storageService;

    public async Task<Domain.Media.Aggregates.Media?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Medias
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
    }

    public async Task<IReadOnlyList<MediaFileDto>> GetByEntityAsync(
        int entityId,
        string entityType,
        CancellationToken ct)
    {
        var rawFiles = await _context.MediaFiles
            .AsNoTracking()
            .Where(m => m.EntityId == entityId && m.EntityType == entityType)
            .Select(m => new { m.Id, m.FilePath, m.ContentType, m.FileName, m.CreatedAt })
            .ToListAsync(ct);

        return rawFiles
            .Select(m => new MediaFileDto(
                m.Id,
                _storageService.GetUrl(m.FilePath),
                m.ContentType,
                m.FileName,
                m.CreatedAt))
            .ToList();
    }

    public async Task<Domain.Media.Aggregates.Media?> GetPrimaryByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default)
    {
        return await _context.Medias
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && m.IsPrimary
                        && !m.IsDeleted)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CountByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default)
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

    public async Task<IReadOnlyList<MediaDto>> GetEntityMediaAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default)
    {
        var medias = await _context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && !m.IsDeleted
                        && m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.FilePath,
                m.AltText,
                m.IsPrimary,
                m.SortOrder
            })
            .ToListAsync(ct);

        return medias.Select(m => new MediaDto
        {
            Id = m.Id,
            Url = _storageService.GetUrl(m.FilePath),
            AltText = m.AltText,
            IsPrimary = m.IsPrimary,
            SortOrder = m.SortOrder
        }).ToList();
    }

    public async Task<string?> GetPrimaryImageUrlAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default)
    {
        var filePath = await _context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && m.IsPrimary
                        && !m.IsDeleted
                        && m.IsActive)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        return filePath != null ? _storageService.GetUrl(filePath) : null;
    }

    public async Task<MediaDetailDto?> GetMediaByIdAsync(
        int mediaId,
        CancellationToken ct = default)
    {
        var media = await _context.Medias
            .AsNoTracking()
            .Where(m => m.Id == mediaId && !m.IsDeleted)
            .Select(m => new
            {
                m.Id,
                m.FilePath,
                m.FileName,
                m.FileType,
                m.FileSize,
                m.EntityType,
                m.EntityId,
                m.AltText,
                m.IsPrimary,
                m.SortOrder,
                m.IsActive,
                m.CreatedAt,
                m.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (media == null) return null;

        return new MediaDetailDto
        {
            Id = media.Id,
            Url = _storageService.GetUrl(media.FilePath),
            FileName = media.FileName,
            FileType = media.FileType,
            FileSize = media.FileSize,
            FileSizeDisplay = FileSizeFormatter.Format(media.FileSize),
            EntityType = media.EntityType,
            EntityId = media.EntityId,
            AltText = media.AltText,
            IsPrimary = media.IsPrimary,
            SortOrder = media.SortOrder,
            IsActive = media.IsActive,
            CreatedAt = media.CreatedAt,
            UpdatedAt = media.UpdatedAt
        };
    }

    public async Task<PaginatedResult<MediaListItemDto>> GetAllMediaPagedAsync(
        string? entityType,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Medias
            .AsNoTracking()
            .Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(m => m.EntityType == entityType);
        }

        var totalItems = await query.CountAsync(ct);

        var medias = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.FilePath,
                m.FileName,
                m.FileType,
                m.FileSize,
                m.EntityType,
                m.EntityId,
                m.IsPrimary,
                m.IsActive,
                m.CreatedAt
            })
            .ToListAsync(ct);

        var dtos = medias.Select(m => new MediaListItemDto
        {
            Id = m.Id,
            Url = _storageService.GetUrl(m.FilePath),
            FileName = m.FileName,
            FileType = m.FileType,
            FileSizeDisplay = FileSizeFormatter.Format(m.FileSize),
            EntityType = m.EntityType,
            EntityId = m.EntityId,
            IsPrimary = m.IsPrimary,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt
        }).ToList();

        return PaginatedResult<MediaListItemDto>.Create(dtos, totalItems, page, pageSize);
    }
}