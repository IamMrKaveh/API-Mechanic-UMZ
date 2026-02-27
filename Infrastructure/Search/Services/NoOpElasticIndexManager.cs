namespace Infrastructure.Search.Services;

public class NoOpElasticIndexManager : IElasticIndexManager
{
    private readonly ILogger<NoOpElasticIndexManager> _logger;

    public NoOpElasticIndexManager(ILogger<NoOpElasticIndexManager> logger)
    {
        _logger = logger;
    }

    public Task<bool> CreateProductIndexAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping CreateProductIndexAsync.");
        return Task.FromResult(false);
    }

    public Task<bool> CreateCategoryIndexAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping CreateCategoryIndexAsync.");
        return Task.FromResult(false);
    }

    public Task<bool> CreateBrandIndexAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping CreateBrandIndexAsync.");
        return Task.FromResult(false);
    }

    public Task<bool> DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping DeleteIndexAsync for index {IndexName}.", indexName);
        return Task.FromResult(false);
    }

    public Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping IndexExistsAsync for index {IndexName}.", indexName);
        return Task.FromResult(false);
    }

    public Task<bool> ReindexAsync(string sourceIndex, string destinationIndex, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping ReindexAsync from {Source} to {Destination}.",
            sourceIndex, destinationIndex);
        return Task.FromResult(false);
    }

    public Task<bool> CreateAllIndicesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping CreateAllIndicesAsync.");
        return Task.FromResult(false);
    }
}