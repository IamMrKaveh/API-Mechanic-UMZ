namespace Application.Search.Contracts;

public interface ISearchDatabaseSyncService
{
    Task SyncAsync(CancellationToken cancellationToken = default);

    Task SyncProductAsync(int productId, CancellationToken ct = default);

    Task SyncAllProductsAsync(CancellationToken ct = default);

    Task SyncCategoryAsync(int categoryId, CancellationToken ct = default);

    Task SyncAllCategoriesAsync(CancellationToken ct = default);

    Task SyncBrandAsync(int BrandId, CancellationToken ct = default);

    Task SyncAllBrandsAsync(CancellationToken ct = default);

    Task FullSyncAsync(CancellationToken ct = default);
}