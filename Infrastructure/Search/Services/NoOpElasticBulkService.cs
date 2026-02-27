namespace Infrastructure.Search.Services;

public class NoOpElasticBulkService : IElasticBulkService
{
    private readonly ILogger<NoOpElasticBulkService> _logger;

    public NoOpElasticBulkService(ILogger<NoOpElasticBulkService> logger)
    {
        _logger = logger;
    }

    public Task<bool> BulkIndexProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping BulkIndexProductsAsync.");
        return Task.FromResult(false);
    }

    public Task<bool> BulkIndexCategoriesAsync(IEnumerable<CategorySearchDocument> categories, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping BulkIndexCategoriesAsync.");
        return Task.FromResult(false);
    }

    public Task<bool> BulkIndexBrandsAsync(IEnumerable<BrandSearchDocument> brands, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping BulkIndexBrandsAsync.");
        return Task.FromResult(false);
    }

    public Task<bool> BulkDeleteProductsAsync(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping BulkDeleteProductsAsync.");
        return Task.FromResult(false);
    }

    public Task<bool> BulkUpdateProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
    {
        _logger.LogDebug("Elasticsearch is disabled. Skipping BulkUpdateProductsAsync.");
        return Task.FromResult(false);
    }
}