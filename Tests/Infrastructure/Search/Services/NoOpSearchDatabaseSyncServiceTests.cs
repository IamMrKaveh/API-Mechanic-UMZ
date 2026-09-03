using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Infrastructure.Search.Services;

namespace Tests.Infrastructure.Search.Services;

public class NoOpSearchDatabaseSyncServiceTests
{
    private readonly NoOpSearchDatabaseSyncService _sut = new();

    [Fact]
    public async Task SyncAsync_CompletesWithoutThrowing()
    {
        await _sut.SyncAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SyncProductAsync_CompletesWithoutThrowing()
    {
        await _sut.SyncProductAsync(ProductId.NewId(), CancellationToken.None);
    }

    [Fact]
    public async Task SyncCategoryAsync_CompletesWithoutThrowing()
    {
        await _sut.SyncCategoryAsync(CategoryId.NewId(), CancellationToken.None);
    }

    [Fact]
    public async Task SyncBrandAsync_CompletesWithoutThrowing()
    {
        await _sut.SyncBrandAsync(BrandId.NewId(), CancellationToken.None);
    }

    [Fact]
    public async Task SyncAllProductsAsync_CompletesWithoutThrowing()
    {
        await _sut.SyncAllProductsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SyncAllCategoriesAsync_CompletesWithoutThrowing()
    {
        await _sut.SyncAllCategoriesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SyncAllBrandsAsync_CompletesWithoutThrowing()
    {
        await _sut.SyncAllBrandsAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FullSyncAsync_CompletesWithoutThrowing()
    {
        await _sut.FullSyncAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AllMethods_HonourCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.SyncAsync(cts.Token);
        await _sut.SyncProductAsync(ProductId.NewId(), cts.Token);
        await _sut.SyncCategoryAsync(CategoryId.NewId(), cts.Token);
        await _sut.SyncBrandAsync(BrandId.NewId(), cts.Token);
        await _sut.SyncAllProductsAsync(cts.Token);
        await _sut.SyncAllCategoriesAsync(cts.Token);
        await _sut.SyncAllBrandsAsync(cts.Token);
        await _sut.FullSyncAsync(cts.Token);
    }
}
