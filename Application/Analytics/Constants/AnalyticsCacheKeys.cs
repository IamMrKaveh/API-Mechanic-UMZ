namespace Application.Analytics.Constants;

public static class AnalyticsCacheKeys
{
    public const string InventoryReport = "analytics:inventory-report";

    public static string Dashboard(DateTime? fromDate, DateTime? toDate) =>
        $"analytics:dashboard:{FormatDate(fromDate)}:{FormatDate(toDate)}";

    public static string CategoryPerformance(DateTime? fromDate, DateTime? toDate) =>
        $"analytics:category-perf:{FormatDate(fromDate)}:{FormatDate(toDate)}";

    public static string RevenueReport(DateTime fromDate, DateTime toDate) =>
        $"analytics:revenue:{FormatDate(fromDate)}:{FormatDate(toDate)}";

    public static string SalesChart(DateTime fromDate, DateTime toDate, string groupBy) =>
        $"analytics:sales-chart:{FormatDate(fromDate)}:{FormatDate(toDate)}:{groupBy}";

    public static string TopSellingProducts(int count, DateTime? fromDate, DateTime? toDate) =>
        $"analytics:top-products:{count}:{FormatDate(fromDate)}:{FormatDate(toDate)}";

    private static string FormatDate(DateTime? date) =>
        date?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty;
}
