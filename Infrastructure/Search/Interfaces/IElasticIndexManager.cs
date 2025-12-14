namespace Infrastructure.Search.Interfaces;

public interface IElasticIndexManager
{
    Task<bool> CreateProductIndexAsync(CancellationToken ct = default);
    Task<bool> CreateCategoryIndexAsync(CancellationToken ct = default);
    Task<bool> CreateCategoryGroupIndexAsync(CancellationToken ct = default);
    Task<bool> DeleteIndexAsync(string indexName, CancellationToken ct = default);
    Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default);
    Task<bool> ReindexAsync(string sourceIndex, string destinationIndex, CancellationToken ct = default);
    Task<bool> CreateAllIndicesAsync(CancellationToken ct = default);
}