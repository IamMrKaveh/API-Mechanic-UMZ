namespace MainApi.Services.Media;

public interface IMediaService
{
    Task<TMedia> AttachFileToEntityAsync(IFormFile file, string entityType, int entityId, bool isPrimary, string? altText = null);
    Task<IEnumerable<TMedia>> GetEntityMediaAsync(string entityType, int entityId);
    Task<bool> DeleteMediaAsync(int mediaId);
    Task<string?> GetPrimaryImageUrlAsync(string entityType, int entityId);
    Task<bool> SetPrimaryMediaAsync(int mediaId, int entityId, string entityType);
}