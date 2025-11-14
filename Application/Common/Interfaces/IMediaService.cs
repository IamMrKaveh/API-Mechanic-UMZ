namespace Application.Common.Interfaces;

public interface IMediaService
{
    Task<Domain.Media.Media> AttachFileToEntityAsync(Stream stream, string fileName, string contentType, long contentLength, string entityType, int entityId, bool isPrimary, string? altText = null);
    Task<IEnumerable<Domain.Media.Media>> GetEntityMediaAsync(string entityType, int entityId);
    Task<bool> DeleteMediaAsync(int mediaId);
    Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId);
    Task<bool> SetPrimaryMediaAsync(int mediaId, int entityId, string entityType);
    Task<List<Domain.Media.Media>> UploadFilesAsync(IEnumerable<(Stream stream, string fileName, string contentType, long contentLength)> files, string entityType, int entityId, bool isPrimary, string? altText);
    Task<IEnumerable<object>> GetMediaForEntityAsync(string entityType, int entityId);
}