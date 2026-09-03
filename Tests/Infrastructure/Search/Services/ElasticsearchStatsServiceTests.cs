using Elastic.Clients.Elasticsearch;
using Infrastructure.Search.Services;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search.Services;

public class ElasticsearchStatsServiceTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private ElasticsearchStatsService _sut = null!;

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        _sut = new ElasticsearchStatsService(_server.CreateClient(), _auditService);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private void RouteHealthAndStats(int healthStatus = 200, int statsStatus = 200) =>
        _server.Router = (_, path) =>
            path.Contains("_cluster/health")
                ? (FakeElasticsearchServer.Bodies.ClusterHealthGreen, healthStatus)
                : path.Contains("_stats")
                    ? (FakeElasticsearchServer.Bodies.IndicesStats(7), statsStatus)
                    : (FakeElasticsearchServer.Bodies.BulkOk, 200);

    [Fact]
    public async Task GetStatsAsync_WhenClusterIsHealthy_ReturnsFullStats()
    {
        RouteHealthAndStats();

        var result = await _sut.GetStatsAsync(CancellationToken.None);

        result.IsAvailable.ShouldBeTrue();
        result.Status.ShouldBe("Green");
        result.TotalDocuments.ShouldBe(7);
        result.ClusterName.ShouldBe("test-cluster");
        result.NumberOfNodes.ShouldBe(2);
        result.ActivePrimaryShards.ShouldBe(10);
        result.UnavailableReason.ShouldBeNull();
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task GetStatsAsync_WhenHealthIsInvalid_ReturnsUnavailableWithReason()
    {
        RouteHealthAndStats(healthStatus: 500);

        var result = await _sut.GetStatsAsync(CancellationToken.None);

        result.IsAvailable.ShouldBeFalse();
        result.UnavailableReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetStatsAsync_WhenStatsIsInvalid_ReturnsAvailableWithZeroDocuments()
    {
        RouteHealthAndStats(statsStatus: 500);

        var result = await _sut.GetStatsAsync(CancellationToken.None);

        result.IsAvailable.ShouldBeTrue();
        result.TotalDocuments.ShouldBe(0);
        result.ClusterName.ShouldBe("test-cluster");
    }

    [Fact]
    public async Task GetStatsAsync_WhenTransportFails_ReturnsUnavailableWithoutThrowing()
    {
        var unreachable = new ElasticsearchStatsService(
            new ElasticsearchClient(new Uri("http://127.0.0.1:9")),
            _auditService);

        var result = await unreachable.GetStatsAsync(CancellationToken.None);

        result.IsAvailable.ShouldBeFalse();
        result.UnavailableReason.ShouldNotBeNullOrWhiteSpace();
    }
}
