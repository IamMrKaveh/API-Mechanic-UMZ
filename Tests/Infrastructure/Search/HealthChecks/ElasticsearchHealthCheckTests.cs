using Infrastructure.Search.HealthChecks;
using Infrastructure.Search.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search.HealthChecks;

public class ElasticsearchHealthCheckTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private ElasticsearchHealthCheck CreateSut() => new(
        _server.CreateClient(),
        Options.Create(new ElasticsearchOptions()),
        _auditService);

    [Fact]
    public async Task CheckHealthAsync_WhenPingSucceeds_ReturnsHealthy()
    {
        _server.Router = (_, _) => ("{}", 200);

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("Elasticsearch is reachable");
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingFails_ReturnsUnhealthyWithoutAuditLog()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Elasticsearch ping failed");
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServerIsUnreachable_ReturnsUnhealthyWithoutAuditLog()
    {
        var unreachable = new ElasticsearchHealthCheck(
            new Elastic.Clients.Elasticsearch.ElasticsearchClient(new Uri("http://127.0.0.1:9")),
            Options.Create(new ElasticsearchOptions()),
            _auditService);

        var result = await unreachable.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Elasticsearch ping failed");
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientThrows_ReturnsUnhealthyAndLogsError()
    {
        _server.Router = (_, _) => (FakeElasticsearchServer.Bodies.Error500, 500);
        var settings = new Elastic.Clients.Elasticsearch.ElasticsearchClientSettings(new Uri(_server.BaseUrl))
            .ThrowExceptions();
        var throwing = new ElasticsearchHealthCheck(
            new Elastic.Clients.Elasticsearch.ElasticsearchClient(settings),
            Options.Create(new ElasticsearchOptions()),
            _auditService);

        var result = await throwing.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("Elasticsearch health check failed")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_ReturnsUnhealthy()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext(), cts.Token);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_HonoursCancellationTokenOverload()
    {
        _server.Router = (_, _) => ("{}", 200);

        var result = await CreateSut().CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}
