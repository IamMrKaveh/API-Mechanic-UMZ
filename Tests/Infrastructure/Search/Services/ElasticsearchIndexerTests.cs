using Infrastructure.Search.Services;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search.Services;

public class ElasticsearchIndexerTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private ElasticsearchIndexer _sut = null!;

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        _sut = new ElasticsearchIndexer(_server.CreateClient(), _auditService);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private static string Doc(string name = "Brake Pad") =>
        "{\"name\":\"" + name + "\"}";

    [Fact]
    public async Task IndexDocumentAsync_WithUnknownEntityType_ReturnsFalseAndLogsError()
    {
        var result = await _sut.IndexDocumentAsync("Spaceship", Guid.NewGuid(), Doc(), "Create");

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("Unknown entity type 'Spaceship'")), Arg.Any<CancellationToken>());
        _server.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("product")]
    [InlineData("PRODUCT")]
    public async Task IndexDocumentAsync_WithUnmappedCasingOrBlankType_ReturnsFalse(string entityType)
    {
        var result = await _sut.IndexDocumentAsync(entityType, Guid.NewGuid(), Doc(), "Create");

        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Product", "products_v1")]
    [InlineData("Category", "categories_v1")]
    [InlineData("Brand", "brands_v1")]
    public async Task IndexDocumentAsync_WithKnownEntityType_RoutesToExpectedIndex(
        string entityType, string expectedIndex)
    {
        var entityId = Guid.NewGuid();
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.IndexCreated(entityId.ToString()), 200);

        var result = await _sut.IndexDocumentAsync(entityType, entityId, Doc(), "Create");

        result.ShouldBeTrue();
        _server.Requests.Count.ShouldBe(1);
        _server.Requests[0].Path.ShouldContain(expectedIndex);
        _server.Requests[0].Path.ShouldContain(entityId.ToString());
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("delete")]
    [InlineData("DELETE")]
    public async Task IndexDocumentAsync_WithDeleteChangeType_IssuesDeleteRequest(string changeType)
    {
        var entityId = Guid.NewGuid();
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Deleted(entityId.ToString()), 200);

        var result = await _sut.IndexDocumentAsync("Product", entityId, Doc(), changeType);

        result.ShouldBeTrue();
        _server.Requests.Count.ShouldBe(1);
        _server.Requests[0].Method.ShouldBe("DELETE");
        _server.Requests[0].Path.ShouldContain(entityId.ToString());
    }

    [Fact]
    public async Task IndexDocumentAsync_WhenDeleteTargetIsAlreadyGone_ReturnsTrue()
    {
        var entityId = Guid.NewGuid();
        _server.Router = (_, _) => (
            "{\"_index\":\"products_v1\",\"_id\":\"" + entityId + "\",\"_version\":1,\"result\":\"not_found\",\"_shards\":{\"total\":1,\"successful\":1,\"failed\":0}}", 404);

        var result = await _sut.IndexDocumentAsync("Product", entityId, Doc(), "Delete");

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Upsert")]
    [InlineData("")]
    public async Task IndexDocumentAsync_WithNonDeleteChangeType_IssuesIndexRequest(string changeType)
    {
        var entityId = Guid.NewGuid();
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.IndexCreated(entityId.ToString()), 200);

        var result = await _sut.IndexDocumentAsync("Product", entityId, Doc(), changeType);

        result.ShouldBeTrue();
        _server.Requests[0].Method.ShouldBe("PUT");
    }

    [Fact]
    public async Task IndexDocumentAsync_WhenIndexingFails_ReturnsFalseAndLogsError()
    {
        var entityId = Guid.NewGuid();
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await _sut.IndexDocumentAsync("Product", entityId, Doc(), "Create");

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains($"Product:{entityId}")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IndexDocumentAsync_WithInvalidJson_ReturnsFalseAndLogsErrorWithoutRequest()
    {
        var result = await _sut.IndexDocumentAsync("Product", Guid.NewGuid(), "not-json{{{", "Create");

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _server.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task IndexDocumentAsync_WhenServerIsUnreachable_ReturnsFalseAndLogsError()
    {
        var unreachable = new ElasticsearchIndexer(
            new Elastic.Clients.Elasticsearch.ElasticsearchClient(new Uri("http://127.0.0.1:9")),
            _auditService);

        var result = await unreachable.IndexDocumentAsync("Product", Guid.NewGuid(), Doc(), "Create");

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IndexDocumentAsync_WithCancelledToken_ReturnsFalse()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.IndexDocumentAsync("Product", Guid.NewGuid(), Doc(), "Create", cts.Token);

        result.ShouldBeFalse();
    }
}
