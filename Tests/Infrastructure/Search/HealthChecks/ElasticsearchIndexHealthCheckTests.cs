using Infrastructure.Search.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search.HealthChecks;

public class ElasticsearchIndexHealthCheckTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private ElasticsearchIndexHealthCheck CreateSut() => new(_server.CreateClient(), _auditService);

    [Fact]
    public async Task CheckHealthAsync_WhenAllIndicesExist_ReturnsHealthy()
    {
        _server.Router = (_, _) => ("{}", 200);

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldContain("products_v1");
        result.Description.ShouldContain("categories_v1");
        result.Description.ShouldContain("brands_v1");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSingleIndexIsMissing_ReturnsDegraded()
    {
        _server.Router = (_, path) => path.Contains("brands_v1") ? ("{}", 404) : ("{}", 200);

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldContain("brands_v1");
        result.Description.ShouldContain("Missing required indices");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenAllIndicesAreMissing_ReturnsUnhealthy()
    {
        _server.Router = (_, _) => ("{}", 404);

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("All required indices are missing");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExistenceCheckThrows_ReturnsUnhealthyAndLogsPerIndex()
    {
        var settings = new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(new Uri(_server.BaseUrl))
            .ThrowExceptions();
        var throwing = new ElasticsearchIndexHealthCheck(
            new Elastic.Clients.Elasticsearch.ElasticsearchClient(settings),
            _auditService);
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await throwing.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldContain("connectivity failures");
        await _auditService.Received(3).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("index existence check failed")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServerIsUnreachable_ReturnsUnhealthy()
    {
        var unreachable = new ElasticsearchIndexHealthCheck(
            new Elastic.Clients.Elasticsearch.ElasticsearchClient(new Uri("http://127.0.0.1:9")),
            _auditService);

        var result = await unreachable.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_ReturnsUnhealthy()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext(), cts.Token);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
