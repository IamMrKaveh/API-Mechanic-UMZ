using Infrastructure.Analytics.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Database;

namespace Tests.Integration.Analytics;

[Collection(nameof(DatabaseCollection))]
[Trait("Category", "Integration")]
public class GetDashboardStatisticsIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private AnalyticsQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new AnalyticsQueryService(_context);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [RequiresDockerFact]
    public async Task GetDashboardStatisticsAsync_EmptyDatabase_ReturnsAllZeroTotals()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var result = await _sut.GetDashboardStatisticsAsync(from, to, CancellationToken.None);

        result.ShouldNotBeNull();
        result.TotalOrders.ShouldBe(0);
        result.TotalRevenue.ShouldBe(0m);
        result.NewUsersInPeriod.ShouldBe(0);
        result.TotalUsers.ShouldBe(0);
        result.TotalProducts.ShouldBe(0);
    }

    [RequiresDockerFact]
    public async Task GetDashboardStatisticsAsync_NullFromAndTo_UsesDefaultLast30DaysWindow()
    {
        var result = await _sut.GetDashboardStatisticsAsync(from: null, to: null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.TotalOrders.ShouldBe(0);
        result.TotalRevenue.ShouldBe(0m);
        result.NewUsersInPeriod.ShouldBe(0);
        result.TotalUsers.ShouldBe(0);
        result.TotalProducts.ShouldBe(0);
    }
}
