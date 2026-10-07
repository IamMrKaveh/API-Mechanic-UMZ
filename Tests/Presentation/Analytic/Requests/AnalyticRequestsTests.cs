using Presentation.Analytic.Requests;

namespace Tests.Presentation.Analytic.Requests;

public class AnalyticRequestsTests
{
    [Fact]
    public void GetDashboardStatisticsRequest_WithDefaultValues_HasNullDates()
    {
        var request = new GetDashboardStatisticsRequest();

        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetDashboardStatisticsRequest_WithProvidedValues_SetsCorrectly()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetDashboardStatisticsRequest(fromDate, toDate);

        request.FromDate.ShouldBe(fromDate);
        request.ToDate.ShouldBe(toDate);
    }

    [Fact]
    public void GetDashboardStatisticsRequest_WithOnlyFromDate_SetsCorrectly()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetDashboardStatisticsRequest(fromDate, null);

        request.FromDate.ShouldBe(fromDate);
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetDashboardStatisticsRequest_WithOnlyToDate_SetsCorrectly()
    {
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetDashboardStatisticsRequest(null, toDate);

        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBe(toDate);
    }

    [Fact]
    public void GetDashboardStatisticsRequest_IsRecord_EqualityWorks()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request1 = new GetDashboardStatisticsRequest(fromDate, toDate);
        var request2 = new GetDashboardStatisticsRequest(fromDate, toDate);
        var request3 = new GetDashboardStatisticsRequest(toDate, fromDate);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void GetSalesChartDataRequest_WithAllParameters_SetsCorrectly()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetSalesChartDataRequest(fromDate, toDate, "week");

        request.FromDate.ShouldBe(fromDate);
        request.ToDate.ShouldBe(toDate);
        request.GroupBy.ShouldBe("week");
    }

    [Fact]
    public void GetSalesChartDataRequest_WithDefaultGroupBy_UsesDay()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetSalesChartDataRequest(fromDate, toDate);

        request.GroupBy.ShouldBe("day");
    }

    [Fact]
    public void GetSalesChartDataRequest_IsRecord_EqualityWorks()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request1 = new GetSalesChartDataRequest(fromDate, toDate, "day");
        var request2 = new GetSalesChartDataRequest(fromDate, toDate, "day");
        var request3 = new GetSalesChartDataRequest(fromDate, toDate, "week");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void GetTopSellingProductsRequest_WithDefaultValues_SetsCorrectly()
    {
        var request = new GetTopSellingProductsRequest();

        request.Count.ShouldBe(10);
        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetTopSellingProductsRequest_WithAllParameters_SetsCorrectly()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetTopSellingProductsRequest(5, fromDate, toDate);

        request.Count.ShouldBe(5);
        request.FromDate.ShouldBe(fromDate);
        request.ToDate.ShouldBe(toDate);
    }

    [Fact]
    public void GetTopSellingProductsRequest_IsRecord_EqualityWorks()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request1 = new GetTopSellingProductsRequest(5, fromDate, toDate);
        var request2 = new GetTopSellingProductsRequest(5, fromDate, toDate);
        var request3 = new GetTopSellingProductsRequest(10, fromDate, toDate);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void GetCategoryPerformanceRequest_WithDefaultValues_HasNullDates()
    {
        var request = new GetCategoryPerformanceRequest();

        request.FromDate.ShouldBeNull();
        request.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetCategoryPerformanceRequest_WithAllParameters_SetsCorrectly()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetCategoryPerformanceRequest(fromDate, toDate);

        request.FromDate.ShouldBe(fromDate);
        request.ToDate.ShouldBe(toDate);
    }

    [Fact]
    public void GetCategoryPerformanceRequest_IsRecord_EqualityWorks()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request1 = new GetCategoryPerformanceRequest(fromDate, toDate);
        var request2 = new GetCategoryPerformanceRequest(fromDate, toDate);
        var request3 = new GetCategoryPerformanceRequest(toDate, fromDate);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void GetRevenueReportRequest_WithAllParameters_SetsCorrectly()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request = new GetRevenueReportRequest(fromDate, toDate);

        request.FromDate.ShouldBe(fromDate);
        request.ToDate.ShouldBe(toDate);
    }

    [Fact]
    public void GetRevenueReportRequest_IsRecord_EqualityWorks()
    {
        var fromDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);

        var request1 = new GetRevenueReportRequest(fromDate, toDate);
        var request2 = new GetRevenueReportRequest(fromDate, toDate);
        var request3 = new GetRevenueReportRequest(toDate, fromDate);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}