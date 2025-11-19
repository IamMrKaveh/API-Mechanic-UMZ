namespace Application.Common.Interfaces.Persistence;

public interface IMediaRepository
{
    Task<Media?> GetByIdAsync(int id);
    Task<IEnumerable<Media>> GetByEntityAsync(string entityType, int entityId);
    Task AddAsync(Media media);
    void Remove(Media media);
    void SetOriginalRowVersion(Media entity, byte[] rowVersion);
    Task<Media?> GetPrimaryMediaByEntityAsync(string entityType, int entityId);
}