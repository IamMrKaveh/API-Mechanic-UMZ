using Application.Search.Features.Shared;
using Elastic.Clients.Elasticsearch;
using Infrastructure.Search.Services;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search.Services;

public class ElasticsearchServiceTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private ElasticsearchService _sut = null!;

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        _sut = new ElasticsearchService(_server.CreateClient(), _auditService);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private static ProductSearchDocument NewProduct(string name = "Brake Pad") => new()
    {
        ProductId = Guid.NewGuid(),
        Name = name,
        Price = 150_000m
    };

    private static string HitSource(Guid id, string name, long price) =>
        "{\"productId\":\"" + id + "\",\"name\":\"" + name + "\",\"price\":" + price + "}";

    private void RouteSearch(string hitsJson, long total) =>
        _server.Router = (_, path) => path.Contains("_search")
            ? (FakeElasticsearchServer.Bodies.SearchHits(hitsJson, total), 200)
            : (FakeElasticsearchServer.Bodies.BulkOk, 200);

    [Fact]
    public async Task IndexProductAsync_WhenValid_CompletesWithoutErrorLog()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var document = NewProduct();

        await _sut.IndexProductAsync(document, CancellationToken.None);

        _server.Requests.Count.ShouldBe(1);
        _server.Requests[0].Path.ShouldContain("products_v1");
        _server.Requests[0].Body.ShouldContain(document.ProductId.ToString());
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task IndexProductAsync_WhenInvalid_LogsErrorWithProductId()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var document = NewProduct();

        await _sut.IndexProductAsync(document, CancellationToken.None);

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains(document.ProductId.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IndexCategoryAsync_WhenValid_CompletesWithoutErrorLog()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var document = new CategorySearchDocument { CategoryId = Guid.NewGuid(), Name = "Brakes" };

        await _sut.IndexCategoryAsync(document, CancellationToken.None);

        _server.Requests[0].Path.ShouldContain("categories_v1");
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task IndexCategoryAsync_WhenInvalid_LogsErrorWithCategoryId()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var document = new CategorySearchDocument { CategoryId = Guid.NewGuid(), Name = "Brakes" };

        await _sut.IndexCategoryAsync(document, CancellationToken.None);

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains(document.CategoryId.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IndexBrandAsync_WhenValid_CompletesWithoutErrorLog()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var document = new BrandSearchDocument { BrandId = Guid.NewGuid(), Name = "Brembo" };

        await _sut.IndexBrandAsync(document, CancellationToken.None);

        _server.Requests[0].Path.ShouldContain("brands_v1");
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task IndexBrandAsync_WhenInvalid_LogsErrorWithBrandId()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var document = new BrandSearchDocument { BrandId = Guid.NewGuid(), Name = "Brembo" };

        await _sut.IndexBrandAsync(document, CancellationToken.None);

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains(document.BrandId.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProductAsync_DeletesDocumentById()
    {
        string? seenPath = null;
        _server.Router = (_, path) =>
        {
            seenPath = path;
            return (FakeElasticsearchServer.Bodies.Deleted("x"), 200);
        };
        var productId = Guid.NewGuid();

        await _sut.DeleteProductAsync(productId, CancellationToken.None);

        seenPath.ShouldNotBeNull();
        seenPath!.ShouldContain("products_v1");
        seenPath.ShouldContain(productId.ToString());
    }

    [Fact]
    public async Task SearchProductsAsync_WhenValid_MapsItemsTotalAndPaging()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        RouteSearch(
            "[" + FakeElasticsearchServer.Bodies.SearchHit("1", HitSource(firstId, "Brake Pad", 150000))
            + "," + FakeElasticsearchServer.Bodies.SearchHit("2", HitSource(secondId, "Oil Filter", 90000)) + "]",
            total: 2);

        var result = await _sut.SearchProductsAsync(
            new SearchProductsParams { Q = "brake", Page = 2, PageSize = 10 },
            CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.Items[0].ProductId.ShouldBe(firstId);
        result.Items[0].Name.ShouldBe("Brake Pad");
        result.Items[0].Price.ShouldBe(150000m);
        result.Items[1].ProductId.ShouldBe(secondId);
        result.Total.ShouldBe(2);
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(10);
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task SearchProductsAsync_WhenInvalid_ReturnsEmptyAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.SearchProductsAsync(
            new SearchProductsParams { Q = "brake" }, CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.Total.ShouldBe(0);
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("SearchProductsAsync failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchGlobalAsync_DelegatesToProductSearchAndEchoesQuery()
    {
        var id = Guid.NewGuid();
        RouteSearch(
            "[" + FakeElasticsearchServer.Bodies.SearchHit("1", HitSource(id, "Brake Pad", 150000)) + "]",
            total: 1);

        var result = await _sut.SearchGlobalAsync("brake", CancellationToken.None);

        result.Query.ShouldBe("brake");
        result.Products.Count.ShouldBe(1);
        result.Products[0].ProductId.ShouldBe(id);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WhenValid_ReturnsDistinctNonEmptyNames()
    {
        RouteSearch(
            "[" + FakeElasticsearchServer.Bodies.SearchHit("1", HitSource(Guid.NewGuid(), "Brake Pad", 1))
            + "," + FakeElasticsearchServer.Bodies.SearchHit("2", HitSource(Guid.NewGuid(), "Brake Pad", 1))
            + "," + FakeElasticsearchServer.Bodies.SearchHit("3", HitSource(Guid.NewGuid(), "Brake Rotor", 1)) + "]",
            total: 3);

        var result = await _sut.GetSuggestionsAsync("bra", maxSuggestions: 10, CancellationToken.None);

        result.ShouldBe(["Brake Pad", "Brake Rotor"]);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WhenInvalid_ReturnsEmptyWithoutLogging()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.GetSuggestionsAsync("bra", ct: CancellationToken.None);

        result.ShouldBeEmpty();
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task SearchWithFuzzyAsync_WhenValid_MapsResults()
    {
        var id = Guid.NewGuid();
        RouteSearch(
            "[" + FakeElasticsearchServer.Bodies.SearchHit("1", HitSource(id, "Brake Pad", 150000)) + "]",
            total: 1);

        var result = await _sut.SearchWithFuzzyAsync("brkae", page: 1, pageSize: 5, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Brake Pad");
        result.Total.ShouldBe(1);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(5);
    }

    [Fact]
    public async Task SearchWithFuzzyAsync_WhenInvalid_ReturnsEmptyAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.SearchWithFuzzyAsync("brkae", ct: CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.Total.ShouldBe(0);
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("SearchWithFuzzyAsync failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIndexStatsAsync_WhenValid_ReturnsTotalDocuments()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.IndicesStats(7), 200);

        var result = await _sut.GetIndexStatsAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result!.ProductsCount.ShouldBe(7);
        result.CategoriesCount.ShouldBe(0);
        result.BrandsCount.ShouldBe(0);
        result.TotalDocuments.ShouldBe(0);
    }

    [Fact]
    public async Task GetIndexStatsAsync_WhenInvalid_ReturnsNull()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.GetIndexStatsAsync(CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetIndexStatsAsync_WhenTransportFails_ReturnsNull()
    {
        var unreachable = new ElasticsearchService(
            new ElasticsearchClient(new Uri("http://127.0.0.1:9")),
            _auditService);

        var result = await unreachable.GetIndexStatsAsync(CancellationToken.None);

        result.ShouldBeNull();
    }
}
