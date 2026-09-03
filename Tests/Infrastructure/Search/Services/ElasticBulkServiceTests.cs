using Application.Search.Features.Shared;
using Domain.Product.ValueObjects;
using Elastic.Clients.Elasticsearch;
using Infrastructure.Search;
using Infrastructure.Search.Services;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search.Services;

public class ElasticBulkServiceTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private ElasticBulkService _sut = null!;

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        _sut = new ElasticBulkService(
            _server.CreateClient(),
            _auditService,
            new ElasticsearchMetrics());
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private static ProductSearchDocument NewProduct(string name = "Brake Pad") => new()
    {
        ProductId = Guid.NewGuid(),
        Name = name,
        Price = 150_000m
    };

    private static CategorySearchDocument NewCategory(string name = "Brakes") => new()
    {
        CategoryId = Guid.NewGuid(),
        Name = name
    };

    private static BrandSearchDocument NewBrand(string name = "Brembo") => new()
    {
        BrandId = Guid.NewGuid(),
        Name = name,
        CategoryId = Guid.NewGuid()
    };

    private static ElasticsearchClient RefusedClient() =>
        new(new ElasticsearchClientSettings(new Uri("http://127.0.0.1:9"))
            .RequestTimeout(TimeSpan.FromSeconds(5)));

    [Fact]
    public async Task BulkIndexProductsAsync_WithEmptyCollection_ReturnsTrueWithoutIo()
    {
        var result = await _sut.BulkIndexProductsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests.ShouldBeEmpty();
        await _auditService.DidNotReceiveWithAnyArgs().LogInformationAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task BulkIndexProductsAsync_WhenValid_IndexesAndLogsCount()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var products = new[] { NewProduct(), NewProduct("Oil Filter") };

        var result = await _sut.BulkIndexProductsAsync(products, CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests.Count.ShouldBe(1);
        _server.Requests[0].Path.ShouldContain("_bulk");
        _server.Requests[0].Path.ShouldContain("products_v1");
        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Bulk indexed 2 products")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkIndexProductsAsync_WhenInvalid_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.BulkIndexProductsAsync([NewProduct()], CancellationToken.None);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Bulk index products failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkIndexProductsAsync_WhenTransportFails_ReturnsFalseAndLogsError()
    {
        var unreachable = new ElasticBulkService(
            RefusedClient(), _auditService, new ElasticsearchMetrics());

        var result = await unreachable.BulkIndexProductsAsync([NewProduct()], CancellationToken.None);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Bulk index products failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkIndexCategoriesAsync_WithEmptyCollection_ReturnsTrueWithoutIo()
    {
        var result = await _sut.BulkIndexCategoriesAsync([], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task BulkIndexCategoriesAsync_WhenValid_ReturnsTrueWithoutInfoLog()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);

        var result = await _sut.BulkIndexCategoriesAsync([NewCategory()], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests[0].Path.ShouldContain("categories_v1");
        await _auditService.DidNotReceiveWithAnyArgs().LogInformationAsync(default!, default);
    }

    [Fact]
    public async Task BulkIndexCategoriesAsync_WhenInvalid_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.BulkIndexCategoriesAsync([NewCategory()], CancellationToken.None);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Bulk index categories failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkIndexBrandsAsync_WithEmptyCollection_ReturnsTrueWithoutIo()
    {
        var result = await _sut.BulkIndexBrandsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task BulkIndexBrandsAsync_WhenValid_ReturnsTrue()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);

        var result = await _sut.BulkIndexBrandsAsync([NewBrand()], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests[0].Path.ShouldContain("brands_v1");
    }

    [Fact]
    public async Task BulkIndexBrandsAsync_WhenInvalid_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.BulkIndexBrandsAsync([NewBrand()], CancellationToken.None);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Bulk index brands failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkDeleteProductsAsync_WithEmptyCollection_ReturnsTrueWithoutIo()
    {
        var result = await _sut.BulkDeleteProductsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task BulkDeleteProductsAsync_WhenValid_SendsDeleteOperations()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var id = ProductId.NewId();

        var result = await _sut.BulkDeleteProductsAsync([id], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests[0].Body.ShouldContain(id.Value.ToString());
        _server.Requests[0].Body.ShouldContain("delete");
    }

    [Fact]
    public async Task BulkDeleteProductsAsync_WhenInvalid_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.BulkDeleteProductsAsync([ProductId.NewId()], CancellationToken.None);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Bulk delete products failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_WithEmptyCollection_ReturnsTrueWithoutIo()
    {
        var result = await _sut.BulkUpdateProductsAsync([], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_WhenValid_SendsUpdateOperations()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var product = NewProduct();

        var result = await _sut.BulkUpdateProductsAsync([product], CancellationToken.None);

        result.ShouldBeTrue();
        _server.Requests[0].Body.ShouldContain("update");
        _server.Requests[0].Body.ShouldContain(product.ProductId.ToString());
    }

    [Fact]
    public async Task BulkUpdateProductsAsync_WhenInvalid_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.BulkUpdateProductsAsync([NewProduct()], CancellationToken.None);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Bulk update products failed")),
            Arg.Any<CancellationToken>());
    }
}
