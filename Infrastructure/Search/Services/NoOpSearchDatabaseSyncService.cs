namespace Infrastructure.Search.Services;

public class NoOpSearchDatabaseSyncService : ISearchDatabaseSyncService
{
    private readonly ILogger<NoOpSearchDatabaseSyncService> _logger;

    public NoOpSearchDatabaseSyncService(ILogger<NoOpSearchDatabaseSyncService> logger)
    {
        _logger = logger;
    }

    public Task SyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping SyncAsync.");
        return Task.CompletedTask;
    }

    public Task SyncProductAsync(int productId, CancellationToken ct = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping SyncProductAsync for Product {ProductId}.", productId);
        return Task.CompletedTask;
    }

    public Task SyncAllProductsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping SyncAllProductsAsync.");
        return Task.CompletedTask;
    }

    public Task SyncCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping SyncCategoryAsync for Category {CategoryId}.", categoryId);
        return Task.CompletedTask;
    }

    public Task SyncAllCategoriesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping SyncAllCategoriesAsync.");
        return Task.CompletedTask;
    }

    public Task SyncBrandAsync(int brandId, CancellationToken ct = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping SyncBrandAsync for Brand {BrandId}.", brandId);
        return Task.CompletedTask;
    }

    public Task SyncAllBrandsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping SyncAllBrandsAsync.");
        return Task.CompletedTask;
    }

    public Task FullSyncAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Search sync is disabled. Skipping FullSyncAsync.");
        return Task.CompletedTask;
    }
}