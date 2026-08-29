using Infrastructure.Analytics.QueryServices;
using Infrastructure.Persistence.Context;

namespace Tests.Integration.Analytics;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class GetSalesChartDataIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    public async Task GetSalesChartDataAsync_EmptyDatabase_ReturnsEmptyPaginatedResult(string groupBy)
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var result = await _sut.GetSalesChartDataAsync(from, to, groupBy, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
    }
}
