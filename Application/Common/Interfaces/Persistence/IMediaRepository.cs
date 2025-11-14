namespace Application.Common.Interfaces.Persistence;

public interface IMediaRepository
{
    Task AddMediaAsync(Domain.Media.Media media);
    Task<Domain.Media.Media?> GetMediaByIdAsync(int mediaId);
    Task<IEnumerable<Domain.Media.Media>> GetMediaForEntityAsync(string entityType, int entityId);
    Task<string?> GetPrimaryMediaFilePathAsync(string entityType, int entityId);
    void DeleteMedia(Domain.Media.Media media);
    Task UnsetPrimaryMediaAsync(string entityType, int entityId, int? excludeMediaId = null);
}