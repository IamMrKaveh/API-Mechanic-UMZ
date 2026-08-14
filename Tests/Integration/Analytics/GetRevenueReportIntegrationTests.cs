using Application.Analytics.Features.Shared; using Infrastructure.Analytics.QueryServices; using Infrastructure.Persistence.Context; using Tests.TestInfrastructure.Database;

namespace Tests.Integration.Analytics;

[Collection(nameof(DatabaseCollection))] [Trait("Category", "Integration")] public class GetRevenueReportIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime { private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private AnalyticsQueryService _sut = null!;

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

[SkippableFact]
public async Task GetRevenueReportAsync_EmptyDatabase_ReturnsZeroTotalsAndEmptyByStatus()
{
    var from = DateTime.UtcNow.AddDays(-30);
    var to = DateTime.UtcNow;

    var result = await _sut.GetRevenueReportAsync(from, to, CancellationToken.None);

    result.ShouldNotBeNull();
    result.FromDate.ShouldBe(from);
    result.ToDate.ShouldBe(to);
    result.GrossRevenue.ShouldBe(0m);
    result.TotalDiscounts.ShouldBe(0m);
    result.TotalShippingIncome.ShouldBe(0m);
    result.NetRevenue.ShouldBe(0m);
    result.TotalOrders.ShouldBe(0);
    result.AverageOrderValue.ShouldBe(0m);
    result.ByStatus.ShouldNotBeNull();
    result.ByStatus.ShouldBeEmpty();
}
}