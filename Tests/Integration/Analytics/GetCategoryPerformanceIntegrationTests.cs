using Infrastructure.Analytics.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Database;

namespace Tests.Integration.Analytics;

[Collection(nameof(DatabaseCollection))]
[Trait("Category", "Integration")]
public class GetCategoryPerformanceIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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
    public async Task GetCategoryPerformanceAsync_EmptyDatabase_ReturnsEmptyPaginatedResult()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var result = await _sut.GetCategoryPerformanceAsync(from, to, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
    }

    [RequiresDockerFact]
    public async Task GetCategoryPerformanceAsync_NullDateRangeOnEmptyDatabase_ReturnsEmptyPaginatedResult()
    {
        var result = await _sut.GetCategoryPerformanceAsync(null, null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }
}
