namespace Application.Analytics.Features.Shared;

// ─── Dashboard ───

public sealed class DashboardStatisticsDto
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public int ProcessingOrders { get; init; }
    public int ShippedOrders { get; init; }
    public int DeliveredOrders { get; init; }
    public int CancelledOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalProfit { get; init; }
    public decimal AverageOrderValue { get; init; }
    public int TotalUsers { get; init; }
    public int NewUsersInPeriod { get; init; }
    public int TotalProducts { get; init; }
    public int ActiveProducts { get; init; }
    public int OutOfStockVariants { get; init; }
    public int LowStockVariants { get; init; }
    public decimal CancellationRate { get; init; }
    public decimal ProfitMargin { get; init; }
}

// ─── Sales Chart ───

public sealed class SalesChartDataPointDto
{
    public string Label { get; init; } = null!;
    public DateTime Date { get; init; }
    public int OrderCount { get; init; }
    public decimal Revenue { get; init; }
    public decimal Profit { get; init; }
    public int ItemsSold { get; init; }
}

// ─── Top Selling Products ───

public sealed class TopSellingProductDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? Sku { get; init; }
    public int TotalQuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalProfit { get; init; }
    public int OrderCount { get; init; }
    public decimal AverageSellingPrice { get; init; }
}

// ─── Category Performance ───

public sealed class CategoryPerformanceDto
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public int TotalGroups { get; init; }
    public int TotalProducts { get; init; }
    public int TotalQuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalProfit { get; init; }
    public decimal RevenuePercentage { get; init; }
    public int OrderCount { get; init; }
}

// ─── Revenue Report ───

public sealed class RevenueReportDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal TotalDiscounts { get; init; }
    public decimal TotalShippingIncome { get; init; }
    public decimal NetRevenue { get; init; }
    public decimal TotalCost { get; init; }
    public decimal GrossProfit { get; init; }
    public decimal ProfitMargin { get; init; }
    public int TotalOrders { get; init; }
    public int TotalItemsSold { get; init; }
    public decimal AverageOrderValue { get; init; }
    public IReadOnlyList<RevenueByStatusDto> ByStatus { get; init; } = [];
}

public sealed class RevenueByStatusDto
{
    public string Status { get; init; } = null!;
    public int Count { get; init; }
    public decimal Amount { get; init; }
}

// ─── Inventory Report ───

public sealed class InventoryReportDto
{
    public int TotalVariants { get; init; }
    public int ActiveVariants { get; init; }
    public int InStockVariants { get; init; }
    public int OutOfStockVariants { get; init; }
    public int LowStockVariants { get; init; }
    public int UnlimitedVariants { get; init; }
    public decimal TotalStockValue { get; init; }
    public decimal TotalRetailValue { get; init; }
    public decimal PotentialProfit { get; init; }
    public IReadOnlyList<InventoryCategoryBreakdownDto> ByCategory { get; init; } = [];
    public IReadOnlyList<AnalyticsLowStockItemDto> LowStockItems { get; init; } = [];
    public IReadOnlyList<AnalyticsOutOfStockItemDto> OutOfStockItems { get; init; } = [];
}

public sealed class InventoryCategoryBreakdownDto
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public int VariantCount { get; init; }
    public int TotalStock { get; init; }
    public decimal StockValue { get; init; }
}

public sealed class AnalyticsLowStockItemDto
{
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? Sku { get; init; }
    public int AvailableStock { get; init; }
    public int LowStockThreshold { get; init; }
}

public sealed class AnalyticsOutOfStockItemDto
{
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? Sku { get; init; }
    public DateTime? LastStockDate { get; init; }
}