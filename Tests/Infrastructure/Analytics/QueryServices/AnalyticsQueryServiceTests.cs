using Application.Analytics.Contracts;
using Infrastructure.Analytics.QueryServices;
using Tests.TestInfrastructure.Base;
using Inventories = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Infrastructure.Analytics.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AnalyticsQueryServiceTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private IAnalyticsQueryService _sut = null!;

    protected override Task OnInitializeAsync()
    {
        _sut = new AnalyticsQueryService(Context);
        return Task.CompletedTask;
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
        var (brand, category) = await SeedBrandWithCategoryAsync();

        var product1 = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        var product2 = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        Context.Products.AddRange(product1, product2);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
        var (brand, category) = await SeedBrandWithCategoryAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        var v1 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-1").Build();
        var v2 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-2").Build();
        var v3 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-3").Build();
        var v4 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-4").Build();

        Context.Products.Add(product);
        Context.ProductVariants.AddRange(v1, v2, v3, v4);
        await Context.SaveChangesAsync();

        var inStock = Inventories.Create(v1.Id, initialStock: 50);
        var outOfStock = Inventories.Create(v2.Id, initialStock: 0);
        var lowStock = Inventories.Create(v3.Id, initialStock: 3);
        var unlimited = Inventories.Create(v4.Id, initialStock: 0, isUnlimited: true);

        Context.Inventories.AddRange(inStock, outOfStock, lowStock, unlimited);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

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
