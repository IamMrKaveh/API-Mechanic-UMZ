using Application.Media.Features.Shared;
using Domain.Media.ValueObjects;

namespace Application.Media.Contracts;

public interface IMediaQueryService
{
    Task<MediaDto?> GetByIdAsync(
        MediaId id,
        CancellationToken ct = default);

    Task<IReadOnlyList<MediaDto>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    Task<MediaDto?> GetPrimaryByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    Task<PaginatedResult<MediaDto>> GetAllAsync(
        string? entityType,
        int page,
        int pageSize,
        CancellationToken ct = default);
}