using Application.Search.Contracts;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Infrastructure.Search.Services;

public sealed class NoOpSearchDatabaseSyncService() : ISearchDatabaseSyncService
{
    public Task SyncProductAsync(ProductId productId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SyncCategoryAsync(CategoryId categoryId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SyncBrandAsync(BrandId brandId, CancellationToken ct = default)
        => Task.CompletedTask;
}