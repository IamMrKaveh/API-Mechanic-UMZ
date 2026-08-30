using Application.Analytics.Contracts;
using Domain.Variant.ValueObjects;
using Infrastructure.Analytics.QueryServices;
using Infrastructure.Persistence.Context;
using Inventories = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Infrastructure.Analytics.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AnalyticsQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IAnalyticsQueryService _sut = null!;

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
    public async Task GetDashboardStatisticsAsync_EmptyDatabase_ReturnsAllZeros()
    {
        var result = await _sut.GetDashboardStatisticsAsync(null, null);

        result.ShouldNotBeNull();
        result.TotalOrders.ShouldBe(0);
        result.TotalRevenue.ShouldBe(0m);
        result.NewUsersInPeriod.ShouldBe(0);
        result.TotalUsers.ShouldBe(0);
        result.TotalProducts.ShouldBe(0);
    }

    [Fact]
    public async Task GetDashboardStatisticsAsync_WithProductsInDatabase_CountsAllProducts()
    {
        var product1 = new ProductBuilder().Build();
        var product2 = new ProductBuilder().Build();
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetDashboardStatisticsAsync(null, null);

        result.TotalProducts.ShouldBe(2);
    }

    [Fact]
    public async Task GetInventoryReportAsync_EmptyDatabase_ReturnsAllZeros()
    {
        var result = await _sut.GetInventoryReportAsync();

        result.ShouldNotBeNull();
        result.TotalVariants.ShouldBe(0);
        result.InStockVariants.ShouldBe(0);
        result.OutOfStockVariants.ShouldBe(0);
        result.LowStockVariants.ShouldBe(0);
    }

    [Fact]
    public async Task GetInventoryReportAsync_MixedInventories_CountsEachStateCorrectly()
    {
        var inStock = Inventories.Create(VariantId.NewId(), initialStock: 50);
        var outOfStock = Inventories.Create(VariantId.NewId(), initialStock: 0);
        var lowStock = Inventories.Create(VariantId.NewId(), initialStock: 3);
        var unlimited = Inventories.Create(VariantId.NewId(), initialStock: 0, isUnlimited: true);

        _context.Inventories.AddRange(inStock, outOfStock, lowStock, unlimited);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetInventoryReportAsync();

        result.TotalVariants.ShouldBe(4);
        result.InStockVariants.ShouldBe(3);
        result.OutOfStockVariants.ShouldBe(1);
        result.LowStockVariants.ShouldBe(1);
    }

    [Fact]
    public async Task GetRevenueReportAsync_NoOrdersInRange_ReturnsZeros()
    {
        var result = await _sut.GetRevenueReportAsync(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);

        result.ShouldNotBeNull();
        result.GrossRevenue.ShouldBe(0m);
        result.NetRevenue.ShouldBe(0m);
        result.TotalOrders.ShouldBe(0);
        result.AverageOrderValue.ShouldBe(0m);
        result.ByStatus.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetTopSellingProductsAsync_NoOrderItems_ReturnsEmptyResult()
    {
        var result = await _sut.GetTopSellingProductsAsync(
            count: 10,
            fromDate: null,
            toDate: null);

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items.Count.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetCategoryPerformanceAsync_NoOrders_ReturnsEmptyResult()
    {
        var result = await _sut.GetCategoryPerformanceAsync(null, null);

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetSalesChartDataAsync_NoOrdersInRange_ReturnsEmptyResult()
    {
        var result = await _sut.GetSalesChartDataAsync(
            fromDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            toDate: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            groupBy: "day");

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(0);
        result.TotalCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    public async Task GetSalesChartDataAsync_EmptyDatabase_ReturnsEmptyForAllGroupings(string groupBy)
    {
        var result = await _sut.GetSalesChartDataAsync(
            fromDate: DateTime.UtcNow.AddDays(-30),
            toDate: DateTime.UtcNow,
            groupBy: groupBy);

        result.Items.Count.ShouldBe(0);
    }
}
