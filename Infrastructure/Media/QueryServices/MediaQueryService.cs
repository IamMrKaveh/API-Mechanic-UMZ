using Application.Media.Features.Shared;
using Domain.Media.ValueObjects;

namespace Infrastructure.Media.QueryServices;

public sealed class MediaQueryService(
    DBContext context,
    IStorageService storageService) : IMediaQueryService
{
    public async Task<MediaDto?> GetByIdAsync(
        MediaId id,
        CancellationToken ct = default)
    {
        var media = await context.Medias
            .AsNoTracking()
            .Where(m => m.Id == id)
            .FirstOrDefaultAsync(ct);

        return media is null ? null : MapToDto(media);
    }

    public async Task<IReadOnlyList<MediaDto>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var medias = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return medias.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<MediaDto?> GetPrimaryByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var media = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == entityType
                        && m.EntityId == entityId
                        && m.IsPrimary)
            .FirstOrDefaultAsync(ct);

        return media is null ? null : MapToDto(media);
    }

    public async Task<PaginatedResult<MediaDto>> GetAllAsync(
        string? entityType,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Medias.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(m => m.EntityType == entityType);

        var totalItems = await query.CountAsync(ct);

        var medias = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PaginatedResult<MediaDto>.Create(
            medias.Select(MapToDto).ToList(),
            totalItems, page, pageSize);
    }

    private MediaDto MapToDto(Domain.Media.Aggregates.Media media)
    {
        return new MediaDto
        {
            Id = media.Id.Value,
            FilePath = media.Path.Value,
            FileName = media.Path.FileName,
            FileType = media.FileType,
            FileSize = media.Size.Bytes,
            EntityType = media.EntityType,
            EntityId = media.EntityId,
            SortOrder = media.SortOrder,
            IsPrimary = media.IsPrimary,
            AltText = media.AltText,
            IsActive = media.IsActive,
            PublicUrl = storageService.GetPublicUrl(media.Path.Value),
            CreatedAt = media.CreatedAt
        };
    }
}