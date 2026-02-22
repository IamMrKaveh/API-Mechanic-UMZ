namespace Application.Analytics.Contracts;

public interface IAnalyticsQueryService
{
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default
        );

    Task<IReadOnlyList<SalesChartDataPointDto>> GetSalesChartDataAsync(
        DateTime fromDate,
        DateTime toDate,
        string groupBy,
        CancellationToken cancellationToken = default
        );

    Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default
        );

    Task<IReadOnlyList<CategoryPerformanceDto>> GetCategoryPerformanceAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default
        );

    Task<RevenueReportDto> GetRevenueReportAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default
        );

    Task<InventoryReportDto> GetInventoryReportAsync(
        CancellationToken cancellationToken = default
        );
}