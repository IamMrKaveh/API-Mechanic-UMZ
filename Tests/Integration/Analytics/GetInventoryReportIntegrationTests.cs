using Infrastructure.Analytics.QueryServices;
using Infrastructure.Persistence.Context;

namespace Tests.Integration.Analytics;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class GetInventoryReportIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task GetInventoryReportAsync_EmptyDatabase_ReturnsAllZeroCounts()
    {
        var result = await _sut.GetInventoryReportAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result.TotalVariants.ShouldBe(0);
        result.ActiveVariants.ShouldBe(0);
        result.InStockVariants.ShouldBe(0);
        result.OutOfStockVariants.ShouldBe(0);
        result.LowStockVariants.ShouldBe(0);
    }
}
