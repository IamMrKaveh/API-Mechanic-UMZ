namespace Infrastructure.Search.Services;

public sealed class NoOpElasticIndexManager() : IElasticIndexManager
{
    public Task<bool> CreateAllIndicesAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateBrandIndexAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateCategoryIndexAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateProductIndexAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<bool> ReindexAsync(string sourceIndex, string destinationIndex, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}