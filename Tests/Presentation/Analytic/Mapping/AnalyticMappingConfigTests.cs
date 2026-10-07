using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using Mapster;
using Presentation.Analytic.Mapping;
using Presentation.Analytic.Requests;

namespace Tests.Presentation.Analytic.Mapping;

public class AnalyticMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly AnalyticMappingConfig _sut = new();

    public AnalyticMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void GetDashboardStatisticsRequest_MapsToQuery()
    {
        var request = new GetDashboardStatisticsRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = request.Adapt<GetDashboardStatisticsQuery>(_config);

        query.ShouldNotBeNull();
        query.FromDate.ShouldBe(request.FromDate);
        query.ToDate.ShouldBe(request.ToDate);
    }

    [Fact]
    public void GetDashboardStatisticsRequest_WithNullDates_MapsToQuery()
    {
        var request = new GetDashboardStatisticsRequest(null, null);

        var query = request.Adapt<GetDashboardStatisticsQuery>(_config);

        query.ShouldNotBeNull();
        query.FromDate.ShouldBeNull();
        query.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetSalesChartDataRequest_MapsToQuery()
    {
        var request = new GetSalesChartDataRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc),
            "week");

        var query = request.Adapt<GetSalesChartDataQuery>(_config);

        query.ShouldNotBeNull();
        query.FromDate.ShouldBe(request.FromDate);
        query.ToDate.ShouldBe(request.ToDate);
        query.GroupBy.ShouldBe(request.GroupBy);
    }

    [Fact]
    public void GetSalesChartDataRequest_WithDefaultGroupBy_MapsToQuery()
    {
        var request = new GetSalesChartDataRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = request.Adapt<GetSalesChartDataQuery>(_config);

        query.ShouldNotBeNull();
        query.GroupBy.ShouldBe("day");
    }

    [Fact]
    public void GetTopSellingProductsRequest_MapsToQuery()
    {
        var request = new GetTopSellingProductsRequest(
            5,
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = request.Adapt<GetTopSellingProductsQuery>(_config);

        query.ShouldNotBeNull();
        query.Count.ShouldBe(request.Count);
        query.FromDate.ShouldBe(request.FromDate);
        query.ToDate.ShouldBe(request.ToDate);
    }

    [Fact]
    public void GetTopSellingProductsRequest_WithDefaults_MapsToQuery()
    {
        var request = new GetTopSellingProductsRequest();

        var query = request.Adapt<GetTopSellingProductsQuery>(_config);

        query.ShouldNotBeNull();
        query.Count.ShouldBe(10);
        query.FromDate.ShouldBeNull();
        query.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetCategoryPerformanceRequest_MapsToQuery()
    {
        var request = new GetCategoryPerformanceRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = request.Adapt<GetCategoryPerformanceQuery>(_config);

        query.ShouldNotBeNull();
        query.FromDate.ShouldBe(request.FromDate);
        query.ToDate.ShouldBe(request.ToDate);
    }

    [Fact]
    public void GetCategoryPerformanceRequest_WithNullDates_MapsToQuery()
    {
        var request = new GetCategoryPerformanceRequest(null, null);

        var query = request.Adapt<GetCategoryPerformanceQuery>(_config);

        query.ShouldNotBeNull();
        query.FromDate.ShouldBeNull();
        query.ToDate.ShouldBeNull();
    }

    [Fact]
    public void GetRevenueReportRequest_MapsToQuery()
    {
        var request = new GetRevenueReportRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = request.Adapt<GetRevenueReportQuery>(_config);

        query.ShouldNotBeNull();
        query.FromDate.ShouldBe(request.FromDate);
        query.ToDate.ShouldBe(request.ToDate);
    }

    [Fact]
    public void AnalyticMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}