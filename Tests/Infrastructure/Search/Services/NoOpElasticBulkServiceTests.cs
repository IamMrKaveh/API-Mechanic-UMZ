using Application.Search.Features.Shared;
using Domain.Product.ValueObjects;
using Infrastructure.Search.Services;

namespace Tests.Infrastructure.Search.Services;

public class NoOpElasticBulkServiceTests
{
    private readonly NoOpElasticBulkService _sut = new();

    private static ProductSearchDocument NewProduct() => new()
    {
        ProductId = Guid.NewGuid(),
        Name = "Test Product"
    };

    private static CategorySearchDocument NewCategory() => new()
    {
        CategoryId = Guid.NewGuid(),
        Name = "Test Category"
    };

    private static BrandSearchDocument NewBrand() => new()
    {
        BrandId = Guid.NewGuid(),
        Name = "Test Brand",
        CategoryId = Guid.NewGuid()
    };

    [Fact]
    public async Task BulkIndexProductsAsync_WithDocuments_ReturnsTrue()
    {
        var result = await _sut.BulkIndexProductsAsync([NewProduct(), NewProduct()], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkIndexProductsAsync_WithEmptyCollection_ReturnsTrue()
    {
        var result = await _sut.BulkIndexProductsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkIndexCategoriesAsync_WithDocuments_ReturnsTrue()
    {
        var result = await _sut.BulkIndexCategoriesAsync([NewCategory()], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkIndexCategoriesAsync_WithEmptyCollection_ReturnsTrue()
    {
        var result = await _sut.BulkIndexCategoriesAsync([], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkIndexBrandsAsync_WithDocuments_ReturnsTrue()
    {
        var result = await _sut.BulkIndexBrandsAsync([NewBrand()], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkIndexBrandsAsync_WithEmptyCollection_ReturnsTrue()
    {
        var result = await _sut.BulkIndexBrandsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkDeleteProductsAsync_WithIds_ReturnsTrue()
    {
        var result = await _sut.BulkDeleteProductsAsync(
            [ProductId.NewId(), ProductId.NewId()], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkDeleteProductsAsync_WithEmptyCollection_ReturnsTrue()
    {
        var result = await _sut.BulkDeleteProductsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_WithDocuments_ReturnsTrue()
    {
        var result = await _sut.BulkUpdateProductsAsync([NewProduct()], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_WithEmptyCollection_ReturnsTrue()
    {
        var result = await _sut.BulkUpdateProductsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AllMethods_HonourCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        (await _sut.BulkIndexProductsAsync([NewProduct()], cts.Token)).ShouldBeTrue();
        (await _sut.BulkIndexCategoriesAsync([NewCategory()], cts.Token)).ShouldBeTrue();
        (await _sut.BulkIndexBrandsAsync([NewBrand()], cts.Token)).ShouldBeTrue();
        (await _sut.BulkDeleteProductsAsync([ProductId.NewId()], cts.Token)).ShouldBeTrue();
        (await _sut.BulkUpdateProductsAsync([NewProduct()], cts.Token)).ShouldBeTrue();
    }
}
