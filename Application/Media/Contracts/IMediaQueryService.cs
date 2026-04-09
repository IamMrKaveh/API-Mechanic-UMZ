using Application.Media.Features.Shared;

namespace Application.Media.Contracts;

public interface IMediaQueryService
{
    Task<MediaDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<MediaDto>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    Task<MediaDto?> GetPrimaryByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    Task<PaginatedResult<MediaDto>> GetAllAsync(
        string? entityType,
        int page,
        int pageSize,
        CancellationToken ct = default);
}