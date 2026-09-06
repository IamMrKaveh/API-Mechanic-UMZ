using Infrastructure.Search;
using Microsoft.Extensions.Configuration;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search;

public class ElasticIndexManagerTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private const string IndexCreatedAck = """{"acknowledged":true,"shards_acknowledged":true,"index":"x"}""";
    private const string ReindexOk = """{"took":5,"timed_out":false,"total":2,"updated":0,"created":2,"deleted":0,"batches":1,"version_conflicts":0,"noops":0,"retries":{"bulk":0,"search":0},"throttled_millis":0,"requests_per_second":-1,"throttled_until_millis":0,"failures":[]}""";

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private ElasticIndexManager CreateSut()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Elasticsearch:NumberOfShards"] = "1",
                ["Elasticsearch:NumberOfReplicas"] = "0"
            })
            .Build();

        return new ElasticIndexManager(_server.CreateClient(), _auditService, configuration);
    }

    [Fact]
    public async Task CreateProductIndexAsync_WhenIndexAlreadyExists_ReturnsTrueWithoutCreating()
    {
        _server.Router = (_, _) => ("{}", 200);
        var sut = CreateSut();

        var result = await sut.CreateProductIndexAsync();

        result.ShouldBeTrue();
        _server.Requests.Count.ShouldBe(1);
        _server.Requests[0].Method.ShouldBe("HEAD");
        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s.Contains("already exists")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProductIndexAsync_WhenIndexIsMissing_CreatesWithPersianAnalysis()
    {
        _server.Router = (method, _) => method == "HEAD"
            ? ("{}", 404)
            : (IndexCreatedAck, 200);
        var sut = CreateSut();

        var result = await sut.CreateProductIndexAsync();

        result.ShouldBeTrue();
        var create = _server.Requests.Single(r => r.Method == "PUT");
        create.Path.ShouldContain("products_v1");
        create.Body.ShouldContain("persian_advanced");
        create.Body.ShouldContain("persian_autocomplete");
        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s.Contains("Successfully created index products_v1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProductIndexAsync_WhenCreationFails_ReturnsFalseAndLogsError()
    {
        _server.Router = (method, _) => method == "HEAD"
            ? ("{}", 404)
            : (FakeElasticsearchServer.Bodies.Error500, 500);
        var sut = CreateSut();

        var result = await sut.CreateProductIndexAsync();

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("Failed to create index products_v1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProductIndexAsync_WhenServerIsUnreachable_ReturnsFalseAndLogsError()
    {
        var unreachable = new ElasticIndexManager(
            new Elastic.Clients.Elasticsearch.ElasticsearchClient(new Uri("http://127.0.0.1:9")),
            _auditService,
            new ConfigurationBuilder().Build());

        var result = await unreachable.CreateProductIndexAsync();

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategoryIndexAsync_WhenAcknowledged_ReturnsTrue()
    {
        _server.Router = (_, _) => (IndexCreatedAck, 200);
        var sut = CreateSut();

        var result = await sut.CreateCategoryIndexAsync();

        result.ShouldBeTrue();
        _server.Requests[0].Path.ShouldContain("categories_v1");
        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s.Contains("categories_v1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategoryIndexAsync_WhenRejected_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var sut = CreateSut();

        var result = await sut.CreateCategoryIndexAsync();

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("categories_v1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBrandIndexAsync_WhenAcknowledged_ReturnsTrue()
    {
        _server.Router = (_, _) => (IndexCreatedAck, 200);
        var sut = CreateSut();

        var result = await sut.CreateBrandIndexAsync();

        result.ShouldBeTrue();
        _server.Requests[0].Path.ShouldContain("brands_v1");
        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s.Contains("brands_v1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBrandIndexAsync_WhenRejected_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var sut = CreateSut();

        var result = await sut.CreateBrandIndexAsync();

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("brands_v1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteIndexAsync_WhenAcknowledged_ReturnsTrueAndLogsInformation()
    {
        _server.Router = (_, _) => ("""{"acknowledged":true}""", 200);
        var sut = CreateSut();

        var result = await sut.DeleteIndexAsync("products_v1");

        result.ShouldBeTrue();
        _server.Requests[0].Method.ShouldBe("DELETE");
        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s.Contains("Successfully deleted index products_v1")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteIndexAsync_WhenNotFound_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => ("""{"acknowledged":false}""", 404);
        var sut = CreateSut();

        var result = await sut.DeleteIndexAsync("products_v1");

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("Failed to delete index")), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(404, false)]
    public async Task IndexExistsAsync_MapsExistenceStatus(int status, bool expected)
    {
        _server.Router = (_, _) => ("{}", status);
        var sut = CreateSut();

        (await sut.IndexExistsAsync("products_v1")).ShouldBe(expected);
    }

    [Fact]
    public async Task IndexExistsAsync_WhenServerIsUnreachable_ReturnsFalse()
    {
        var sut = new ElasticIndexManager(
            new Elastic.Clients.Elasticsearch.ElasticsearchClient(new Uri("http://127.0.0.1:9")),
            _auditService,
            new ConfigurationBuilder().Build());

        (await sut.IndexExistsAsync("products_v1")).ShouldBeFalse();
    }

    [Fact]
    public async Task ReindexAsync_WhenSucceeded_ReturnsTrueAndLogsInformation()
    {
        _server.Router = (_, _) => (ReindexOk, 200);
        var sut = CreateSut();

        var result = await sut.ReindexAsync("products_v1", "products_v2");

        result.ShouldBeTrue();
        _server.Requests[0].Path.ShouldContain("_reindex");
        _server.Requests[0].Body.ShouldContain("products_v1");
        _server.Requests[0].Body.ShouldContain("products_v2");
        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s.Contains("Reindex from products_v1 to products_v2 succeeded")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReindexAsync_WhenFailed_ReturnsFalseAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var sut = CreateSut();

        var result = await sut.ReindexAsync("products_v1", "products_v2");

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("Reindex failed")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAllIndicesAsync_WhenAllSucceed_ReturnsTrue()
    {
        _server.Router = (method, _) => method == "HEAD" ? ("{}", 404) : (IndexCreatedAck, 200);
        var sut = CreateSut();

        (await sut.CreateAllIndicesAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateAllIndicesAsync_WhenAnyFails_ReturnsFalse()
    {
        _server.Router = (method, path) =>
        {
            if (method == "HEAD") return ("{}", 404);
            return path.Contains("brands_v1")
                ? (FakeElasticsearchServer.Bodies.Error500, 500)
                : (IndexCreatedAck, 200);
        };
        var sut = CreateSut();

        (await sut.CreateAllIndicesAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task Operations_WithCancelledToken_ReturnFalse()
    {
        _server.Router = (_, _) => (IndexCreatedAck, 200);
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        (await sut.CreateProductIndexAsync(cts.Token)).ShouldBeFalse();
        (await sut.CreateCategoryIndexAsync(cts.Token)).ShouldBeFalse();
        (await sut.CreateBrandIndexAsync(cts.Token)).ShouldBeFalse();
        (await sut.DeleteIndexAsync("products_v1", cts.Token)).ShouldBeFalse();
        (await sut.ReindexAsync("a", "b", cts.Token)).ShouldBeFalse();
    }
}
