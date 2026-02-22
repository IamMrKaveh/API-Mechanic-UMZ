namespace Application.Media.Contracts;

public interface IMediaService
{
    Task<Domain.Media.Media> AttachFileToEntityAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string entityType,
        int entityId,
        bool isPrimary = false,
        string? altText = null,
        bool saveChanges = true,
        CancellationToken ct = default);

    Task DeleteMediaAsync(int mediaId, int? deletedBy = null, CancellationToken ct = default);
}