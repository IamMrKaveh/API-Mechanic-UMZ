using Application.Media.Features.Shared;
using Domain.Media.ValueObjects;

namespace Application.Media.Contracts;

public interface IMediaService
{
    Task<ServiceResult<MediaDto>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string entityType,
        int entityId,
        bool isPrimary = false,
        string? altText = null,
        CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(
        MediaId mediaId,
        CancellationToken ct = default);

    Task<ServiceResult> SetAsPrimaryAsync(
        MediaId mediaId,
        CancellationToken ct = default);

    Task<ServiceResult> ReorderAsync(
        string entityType,
        int entityId,
        ICollection<Guid> orderedIds,
        CancellationToken ct = default);
}