namespace Application.Common.Interfaces;

public interface IAnalyticsService
{
    Task<object> GetDashboardStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<object>> GetSalesChartDataAsync(DateTime fromDate, DateTime toDate, string groupBy = "day");
    Task<IEnumerable<object>> GetTopSellingProductsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<object>> GetCategoryPerformanceAsync(DateTime? fromDate = null, DateTime? toDate = null);
}