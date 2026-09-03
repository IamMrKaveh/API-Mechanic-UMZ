using Application.Search.Features.Shared;
using Infrastructure.Search;
using Infrastructure.Search.Services;
using Microsoft.Extensions.Configuration;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search.Services;

public class ResilientElasticSearchServiceTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private static IConfiguration BreakerConfig(int failureThreshold, int breakDurationSeconds)
    {
        var configuration = Substitute.For<IConfiguration>();

        var thresholdSection = Substitute.For<IConfigurationSection>();
        thresholdSection.Value.Returns(failureThreshold.ToString(CultureInfo.InvariantCulture));
        thresholdSection.Path.Returns("Elasticsearch:CircuitBreaker:FailureThreshold");
        configuration.GetSection("Elasticsearch:CircuitBreaker:FailureThreshold").Returns(thresholdSection);

        var durationSection = Substitute.For<IConfigurationSection>();
        durationSection.Value.Returns(breakDurationSeconds.ToString(CultureInfo.InvariantCulture));
        durationSection.Path.Returns("Elasticsearch:CircuitBreaker:BreakDurationSeconds");
        configuration.GetSection("Elasticsearch:CircuitBreaker:BreakDurationSeconds").Returns(durationSection);

        return configuration;
    }

    private ResilientElasticSearchService BuildSut(
        int failureThreshold = 5,
        int breakDurationSeconds = 60) =>
        new(
            new ElasticsearchService(_server.CreateClient(), _auditService),
            new ElasticsearchCircuitBreaker(_auditService, BreakerConfig(failureThreshold, breakDurationSeconds)),
            _auditService);

    private static string HitSource(Guid id, string name) =>
        "{\"productId\":\"" + id + "\",\"name\":\"" + name + "\",\"price\":150000}";

    private void RouteSearchOk() =>
        _server.Router = (_, path) => path.Contains("_search")
            ? (FakeElasticsearchServer.Bodies.SearchHits(
                "[" + FakeElasticsearchServer.Bodies.SearchHit("1", HitSource(Guid.NewGuid(), "Brake Pad")) + "]", 1), 200)
            : (FakeElasticsearchServer.Bodies.BulkOk, 200);

    private async Task OpenCircuitAsync(ResilientElasticSearchService sut)
    {
        // A null document makes the inner service throw before any IO,
        // which the resilient wrapper records as a failure.
        await Should.ThrowAsync<Exception>(() =>
            sut.IndexProductAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task IndexProductAsync_WhenClosedAndValid_DelegatesToInner()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var sut = BuildSut();

        await sut.IndexProductAsync(
            new ProductSearchDocument { ProductId = Guid.NewGuid(), Name = "P" },
            CancellationToken.None);

        _server.Requests.Count.ShouldBe(1);
        _server.Requests[0].Path.ShouldContain("products_v1");
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task IndexProductAsync_WhenInnerResponseIsInvalid_DoesNotThrow()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var sut = BuildSut();

        await sut.IndexProductAsync(
            new ProductSearchDocument { ProductId = Guid.NewGuid(), Name = "P" },
            CancellationToken.None);
    }

    [Fact]
    public async Task IndexProductAsync_WhenInnerThrows_RecordsFailureLogsErrorAndRethrows()
    {
        var sut = BuildSut(failureThreshold: 5);

        await Should.ThrowAsync<Exception>(() =>
            sut.IndexProductAsync(null!, CancellationToken.None));

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("IndexProductAsync failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IndexProductAsync_WhenCircuitOpen_SkipsInnerAndLogsWarning()
    {
        var sut = BuildSut(failureThreshold: 1);
        await OpenCircuitAsync(sut);
        _auditService.ClearReceivedCalls();

        await sut.IndexProductAsync(
            new ProductSearchDocument { ProductId = Guid.NewGuid(), Name = "P" },
            CancellationToken.None);

        await _auditService.Received(1).LogWarningAsync(
            Arg.Is<string>(s => s!.Contains("Circuit breaker open")),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task IndexCategoryAsync_WhenCircuitOpen_ReturnsSilentlyWithoutAudit()
    {
        var sut = BuildSut(failureThreshold: 1);
        await OpenCircuitAsync(sut);
        _auditService.ClearReceivedCalls();

        await sut.IndexCategoryAsync(
            new CategorySearchDocument { CategoryId = Guid.NewGuid(), Name = "C" },
            CancellationToken.None);

        await _auditService.DidNotReceiveWithAnyArgs().LogWarningAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task IndexBrandAsync_WhenCircuitOpen_ReturnsSilentlyWithoutAudit()
    {
        var sut = BuildSut(failureThreshold: 1);
        await OpenCircuitAsync(sut);
        _auditService.ClearReceivedCalls();

        await sut.IndexBrandAsync(
            new BrandSearchDocument { BrandId = Guid.NewGuid(), Name = "B" },
            CancellationToken.None);

        await _auditService.DidNotReceiveWithAnyArgs().LogWarningAsync(default!, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task IndexCategoryAsync_WhenInnerThrows_RecordsFailureAndRethrows()
    {
        var sut = BuildSut(failureThreshold: 5);

        await Should.ThrowAsync<Exception>(() =>
            sut.IndexCategoryAsync(null!, CancellationToken.None));

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("IndexCategoryAsync failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProductAsync_DelegatesToInnerService()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.BulkOk, 200);
        var sut = BuildSut();
        var productId = Guid.NewGuid();

        await sut.DeleteProductAsync(productId, CancellationToken.None);

        _server.Requests.Count.ShouldBe(1);
        _server.Requests[0].Path.ShouldContain(productId.ToString());
    }

    [Fact]
    public async Task SearchProductsAsync_DelegatesToInnerService()
    {
        RouteSearchOk();
        var sut = BuildSut();

        var result = await sut.SearchProductsAsync(
            new SearchProductsParams { Q = "brake" }, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Total.ShouldBe(1);
    }

    [Fact]
    public async Task SearchGlobalAsync_WhenCircuitOpen_ReturnsQueryEchoWithoutCallingInner()
    {
        var sut = BuildSut(failureThreshold: 1);
        await OpenCircuitAsync(sut);
        var callsBefore = _server.Requests.Count;

        var result = await sut.SearchGlobalAsync("brake", CancellationToken.None);

        result.Query.ShouldBe("brake");
        result.Products.ShouldBeEmpty();
        _server.Requests.Count.ShouldBe(callsBefore);
    }

    [Fact]
    public async Task SearchGlobalAsync_WhenClosedAndValid_ReturnsMappedResults()
    {
        RouteSearchOk();
        var sut = BuildSut();

        var result = await sut.SearchGlobalAsync("brake", CancellationToken.None);

        result.Query.ShouldBe("brake");
        result.Products.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetSuggestionsAsync_WhenCircuitOpen_ReturnsEmpty()
    {
        var sut = BuildSut(failureThreshold: 1);
        await OpenCircuitAsync(sut);

        var result = await sut.GetSuggestionsAsync("bra", ct: CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSuggestionsAsync_WhenClosedAndValid_ReturnsNames()
    {
        RouteSearchOk();
        var sut = BuildSut();

        var result = await sut.GetSuggestionsAsync("bra", ct: CancellationToken.None);

        result.ShouldBe(["Brake Pad"]);
    }

    [Fact]
    public async Task SearchWithFuzzyAsync_WhenCircuitOpen_ReturnsEmptyResult()
    {
        var sut = BuildSut(failureThreshold: 1);
        await OpenCircuitAsync(sut);

        var result = await sut.SearchWithFuzzyAsync("brkae", ct: CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.Total.ShouldBe(0);
    }

    [Fact]
    public async Task SearchWithFuzzyAsync_WhenClosedAndValid_ReturnsMappedResults()
    {
        RouteSearchOk();
        var sut = BuildSut();

        var result = await sut.SearchWithFuzzyAsync("brkae", ct: CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Total.ShouldBe(1);
    }

    [Fact]
    public async Task GetIndexStatsAsync_DelegatesToInnerService()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.IndicesStats(7), 200);
        var sut = BuildSut();

        var result = await sut.GetIndexStatsAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result!.ProductsCount.ShouldBe(7);
    }
}
