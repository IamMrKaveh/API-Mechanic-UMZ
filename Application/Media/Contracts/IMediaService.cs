using Application.Media.Features.Shared;
using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Media.Contracts;

public interface IMediaService
{
    Task<ServiceResult<MediaDto>> UploadAsync(
        Stream fileStream,
        FilePath filePath,
        FileSize fileSize,
        string entityType,
        Guid entityId,
        bool isPrimary = false,
        string? altText = null,
        CancellationToken ct = default);

    Task<ServiceResult> DeleteAsync(
        MediaId mediaId,
        UserId? deletedBy = null,
        CancellationToken ct = default);

    Task<ServiceResult> SetAsPrimaryAsync(
        MediaId mediaId,
        CancellationToken ct = default);

    Task<ServiceResult> ReorderAsync(
        string entityType,
        Guid entityId,
        ICollection<Guid> orderedIds,
        CancellationToken ct = default);
}