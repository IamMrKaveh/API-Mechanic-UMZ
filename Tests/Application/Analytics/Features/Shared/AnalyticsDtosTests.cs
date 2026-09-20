using Application.Analytics.Features.Shared;

namespace Tests.Application.Analytics.Features.Shared;

public class AnalyticsDtosTests
{
    [Fact]
    public void DashboardStatisticsDto_DefaultValues_AreZero()
    {
        var dto = new DashboardStatisticsDto();

        dto.TotalOrders.ShouldBe(0);
        dto.PendingOrders.ShouldBe(0);
        dto.ProcessingOrders.ShouldBe(0);
        dto.ShippedOrders.ShouldBe(0);
        dto.DeliveredOrders.ShouldBe(0);
        dto.CancelledOrders.ShouldBe(0);
        dto.TotalRevenue.ShouldBe(0m);
        dto.TotalProfit.ShouldBe(0m);
        dto.AverageOrderValue.ShouldBe(0m);
        dto.TotalUsers.ShouldBe(0);
        dto.NewUsersInPeriod.ShouldBe(0);
        dto.TotalProducts.ShouldBe(0);
        dto.ActiveProducts.ShouldBe(0);
        dto.OutOfStockVariants.ShouldBe(0);
        dto.LowStockVariants.ShouldBe(0);
        dto.CancellationRate.ShouldBe(0m);
        dto.ProfitMargin.ShouldBe(0m);
    }

    [Fact]
    public void DashboardStatisticsDto_InitProperties_RoundTrip()
    {
        var dto = new DashboardStatisticsDto
        {
            TotalOrders = 100,
            PendingOrders = 10,
            ProcessingOrders = 20,
            ShippedOrders = 30,
            DeliveredOrders = 35,
            CancelledOrders = 5,
            TotalRevenue = 1_000_000m,
            TotalProfit = 250_000m,
            AverageOrderValue = 10_000m,
            TotalUsers = 500,
            NewUsersInPeriod = 50,
            TotalProducts = 200,
            ActiveProducts = 180,
            OutOfStockVariants = 5,
            LowStockVariants = 12,
            CancellationRate = 0.05m,
            ProfitMargin = 0.25m
        };

        dto.TotalOrders.ShouldBe(100);
        dto.CancelledOrders.ShouldBe(5);
        dto.TotalRevenue.ShouldBe(1_000_000m);
        dto.TotalProfit.ShouldBe(250_000m);
        dto.CancellationRate.ShouldBe(0.05m);
        dto.ProfitMargin.ShouldBe(0.25m);
    }

    [Fact]
    public void DashboardStatisticsDto_WithExpression_PreservesOtherValues()
    {
        var dto = new DashboardStatisticsDto { TotalOrders = 10, TotalRevenue = 500m };

        var updated = dto with { TotalOrders = 20 };

        updated.TotalOrders.ShouldBe(20);
        updated.TotalRevenue.ShouldBe(500m);
    }

    [Fact]
    public void SalesChartDataPointDto_DefaultLabel_IsEmpty()
    {
        var dto = new SalesChartDataPointDto();

        dto.Label.ShouldBe(string.Empty);
        dto.Date.ShouldBe(default);
        dto.OrderCount.ShouldBe(0);
        dto.Revenue.ShouldBe(0m);
        dto.Profit.ShouldBe(0m);
        dto.ItemsSold.ShouldBe(0);
    }

    [Fact]
    public void SalesChartDataPointDto_InitProperties_RoundTrip()
    {
        var date = new DateTime(2026, 1, 15);
        var dto = new SalesChartDataPointDto
        {
            Label = "Jan 15",
            Date = date,
            OrderCount = 7,
            Revenue = 700_000m,
            Profit = 140_000m,
            ItemsSold = 21
        };

        dto.Label.ShouldBe("Jan 15");
        dto.Date.ShouldBe(date);
        dto.OrderCount.ShouldBe(7);
        dto.Revenue.ShouldBe(700_000m);
        dto.Profit.ShouldBe(140_000m);
        dto.ItemsSold.ShouldBe(21);
    }

    [Fact]
    public void TopSellingProductDto_Defaults_AreEmpty()
    {
        var dto = new TopSellingProductDto();

        dto.ProductId.ShouldBe(default(Guid));
        dto.ProductName.ShouldBe(string.Empty);
        dto.Sku.ShouldBeNull();
        dto.TotalQuantitySold.ShouldBe(0);
        dto.TotalRevenue.ShouldBe(0m);
        dto.TotalProfit.ShouldBe(0m);
        dto.OrderCount.ShouldBe(0);
        dto.AverageSellingPrice.ShouldBe(0m);
    }

    [Fact]
    public void TopSellingProductDto_NullableSku_AcceptsValueAndNull()
    {
        var withSku = new TopSellingProductDto { ProductId = Guid.NewGuid(), ProductName = "P", Sku = "SKU-1" };
        withSku.Sku.ShouldBe("SKU-1");

        var withoutSku = withSku with { Sku = null };
        withoutSku.Sku.ShouldBeNull();
        withoutSku.ProductName.ShouldBe("P");
    }

    [Fact]
    public void CategoryPerformanceDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var dto = new CategoryPerformanceDto
        {
            CategoryId = id,
            CategoryName = "Brakes",
            TotalGroups = 3,
            TotalProducts = 40,
            TotalQuantitySold = 120,
            TotalRevenue = 2_000_000m,
            TotalProfit = 400_000m,
            RevenuePercentage = 0.35m,
            OrderCount = 90
        };

        dto.CategoryId.ShouldBe(id);
        dto.CategoryName.ShouldBe("Brakes");
        dto.RevenuePercentage.ShouldBe(0.35m);
        dto.OrderCount.ShouldBe(90);
    }

    [Fact]
    public void RevenueReportDto_DefaultByStatus_IsEmpty()
    {
        var dto = new RevenueReportDto();

        dto.ByStatus.ShouldNotBeNull();
        dto.ByStatus.ShouldBeEmpty();
        dto.GrossRevenue.ShouldBe(0m);
        dto.NetRevenue.ShouldBe(0m);
        dto.TotalOrders.ShouldBe(0);
    }

    [Fact]
    public void RevenueReportDto_WithByStatus_PreservesItems()
    {
        var dto = new RevenueReportDto
        {
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 1, 31),
            GrossRevenue = 1_000m,
            TotalDiscounts = 100m,
            TotalShippingIncome = 50m,
            NetRevenue = 950m,
            TotalCost = 600m,
            GrossProfit = 350m,
            ProfitMargin = 0.368m,
            TotalOrders = 10,
            TotalItemsSold = 25,
            AverageOrderValue = 95m,
            ByStatus = new List<RevenueByStatusDto>
            {
                new() { Status = "Delivered", Count = 8, Amount = 800m },
                new() { Status = "Cancelled", Count = 2, Amount = 200m }
            }
        };

        dto.ByStatus.Count.ShouldBe(2);
        dto.ByStatus[0].Status.ShouldBe("Delivered");
        dto.NetRevenue.ShouldBe(950m);
        dto.GrossProfit.ShouldBe(350m);
    }

    [Fact]
    public void RevenueByStatusDto_DefaultStatus_IsEmpty()
    {
        var dto = new RevenueByStatusDto();

        dto.Status.ShouldBe(string.Empty);
        dto.Count.ShouldBe(0);
        dto.Amount.ShouldBe(0m);
    }

    [Fact]
    public void InventoryReportDto_InitProperties_RoundTrip()
    {
        var dto = new InventoryReportDto
        {
            TotalVariants = 100,
            ActiveVariants = 90,
            InStockVariants = 70,
            OutOfStockVariants = 10,
            LowStockVariants = 10
        };

        dto.TotalVariants.ShouldBe(100);
        dto.ActiveVariants.ShouldBe(90);
        dto.InStockVariants.ShouldBe(70);
        dto.OutOfStockVariants.ShouldBe(10);
        dto.LowStockVariants.ShouldBe(10);
    }

    [Fact]
    public void DtoRecords_ValueEquality_Works()
    {
        var date = new DateTime(2026, 2, 1);
        var a = new SalesChartDataPointDto { Label = "L", Date = date, OrderCount = 1, Revenue = 10m, Profit = 2m, ItemsSold = 3 };
        var b = new SalesChartDataPointDto { Label = "L", Date = date, OrderCount = 1, Revenue = 10m, Profit = 2m, ItemsSold = 3 };

        a.ShouldBe(b);
    }
}
