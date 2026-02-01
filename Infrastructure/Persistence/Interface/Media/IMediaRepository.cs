namespace Infrastructure.Persistence.Interface.Media;

public interface IMediaRepository
{
    Task<Domain.Media.Media?> GetByIdAsync(int id);
    Task<IEnumerable<Domain.Media.Media>> GetByEntityAsync(string entityType, int entityId);
    Task<Domain.Media.Media?> GetPrimaryMediaByEntityAsync(string entityType, int entityId);
    Task AddAsync(Domain.Media.Media media);
    void Update(Domain.Media.Media media);
    void Remove(Domain.Media.Media media);
    void SetOriginalRowVersion(Domain.Media.Media entity, byte[] rowVersion);
}
