using Application.Analytics.Features.Shared;

namespace Application.Analytics.Contracts;

public interface IAnalyticsQueryService
{
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default);

    Task<IReadOnlyList<SalesChartDataPointDto>> GetSalesChartDataAsync(
        DateTime fromDate,
        DateTime toDate,
        string groupBy,
        CancellationToken ct = default);

    Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default);

    Task<IReadOnlyList<CategoryPerformanceDto>> GetCategoryPerformanceAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default);

    Task<RevenueReportDto> GetRevenueReportAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct = default);

    Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken ct = default);
}