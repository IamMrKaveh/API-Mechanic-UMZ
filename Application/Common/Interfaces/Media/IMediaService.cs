using Application.DTOs.Media;

namespace Application.Common.Interfaces.Media;

public interface IMediaService
{
    Task<IEnumerable<MediaDto>> GetEntityMediaAsync(string entityType, int entityId);
    Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId);
    Task<Media> AttachFileToEntityAsync(Stream stream, string fileName, string contentType, long length, string entityType, int entityId, bool isPrimary = false, string? altText = null, bool saveChanges = true);
    Task<IEnumerable<Media>> UploadFilesAsync(IEnumerable<(Stream stream, string fileName, string contentType, long length)> fileStreams, string entityType, int entityId, bool isPrimary = false, string? altText = null);
    Task<bool> DeleteMediaAsync(int mediaId);
    Task<bool> SetPrimaryMediaAsync(int mediaId, int entityId, string entityType);
    string GetUrl(string? filePath);
}