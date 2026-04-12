namespace Presentation.Analytic.Requests;

public record GetDashboardStatisticsRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null);

public record GetSalesChartDataRequest(
    DateTime FromDate,
    DateTime ToDate,
    string GroupBy = "day");

public record GetTopSellingProductsRequest(
    int Count = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null);

public record GetCategoryPerformanceRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null);

public record GetRevenueReportRequest(
    DateTime FromDate,
    DateTime ToDate);