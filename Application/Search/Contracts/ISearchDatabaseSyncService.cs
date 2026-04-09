using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Application.Search.Contracts;

public interface ISearchDatabaseSyncService
{
    Task SyncAsync(CancellationToken ct = default);

    Task SyncProductAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task SyncAllProductsAsync(CancellationToken ct = default);

    Task SyncCategoryAsync(
        CategoryId categoryId,
        CancellationToken ct = default);

    Task SyncAllCategoriesAsync(CancellationToken ct = default);

    Task SyncBrandAsync(
        BrandId brandId,
        CancellationToken ct = default);

    Task SyncAllBrandsAsync(CancellationToken ct = default);

    Task FullSyncAsync(CancellationToken ct = default);
}