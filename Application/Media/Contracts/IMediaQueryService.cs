namespace Application.Media.Contracts;

public interface IMediaQueryService
{
    Task<IReadOnlyList<MediaDto>> GetEntityMediaAsync(string entityType, int entityId, CancellationToken ct = default);

    Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId, CancellationToken ct = default);

    Task<MediaDetailDto?> GetMediaByIdAsync(int mediaId, CancellationToken ct = default);

    Task<PaginatedResult<MediaListItemDto>> GetAllMediaPagedAsync(string? entityType, int page, int pageSize, CancellationToken ct = default);
}