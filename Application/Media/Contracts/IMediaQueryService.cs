using Application.Common.Models;

namespace Application.Media.Contracts;

public interface IMediaQueryService
{
    Task<int> CountByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    Task<IReadOnlySet<string>> GetAllFilePathsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MediaDto>> GetEntityMediaAsync(string entityType, int entityId, CancellationToken ct = default);

    Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId, CancellationToken ct = default);

    Task<MediaDetailDto?> GetMediaByIdAsync(int mediaId, CancellationToken ct = default);

    Task<PaginatedResult<MediaListItemDto>> GetAllMediaPagedAsync(string? entityType, int page, int pageSize, CancellationToken ct = default);
}