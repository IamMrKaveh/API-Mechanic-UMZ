using Application.Media.Features.Shared;

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

    Task<ServiceResult> DeleteAsync(Guid mediaId, CancellationToken ct = default);

    Task<ServiceResult> SetAsPrimaryAsync(Guid mediaId, CancellationToken ct = default);

    Task<ServiceResult> ReorderAsync(string entityType, int entityId, List<Guid> orderedIds, CancellationToken ct = default);
}