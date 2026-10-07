using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Queries.GetInventoryReport;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using Application.Analytics.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Analytic.Endpoints;
using Presentation.Analytic.Requests;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Analytic.Endpoints;

public class AdminAnalyticsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminAnalyticsController _controller;

    public AdminAnalyticsControllerTests()
    {
        _controller = new AdminAnalyticsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetDashboardStatistics_MapsRequestToQuery_AndReturnsOk()
    {
        var request = new GetDashboardStatisticsRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = new GetDashboardStatisticsQuery(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var expectedDto = new DashboardStatisticsDto { TotalOrders = 42, TotalRevenue = 10000 };

        _mapper.Map<GetDashboardStatisticsQuery>(request).Returns(query);
        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DashboardStatisticsDto>.Success(expectedDto));

        var result = await _controller.GetDashboardStatistics(request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<DashboardStatisticsDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.TotalOrders.ShouldBe(42);
    }

    [Fact]
    public async Task GetDashboardStatistics_WithNullDates_MapsCorrectly()
    {
        var request = new GetDashboardStatisticsRequest(null, null);
        var query = new GetDashboardStatisticsQuery(null, null);
        var expectedDto = new DashboardStatisticsDto();

        _mapper.Map<GetDashboardStatisticsQuery>(request).Returns(query);
        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DashboardStatisticsDto>.Success(expectedDto));

        var result = await _controller.GetDashboardStatistics(request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetDashboardStatisticsQuery>(q => q.FromDate == null && q.ToDate == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSalesChartData_MapsRequestToQuery_AndReturnsOk()
    {
        var request = new GetSalesChartDataRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc),
            "day");

        var query = new GetSalesChartDataQuery(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc),
            "day");

        var expectedDto = new SalesChartDataPointDto { Date = new DateTime(2026, 01, 15), Revenue = 5000 };
        var paginatedResult = new PaginatedResult<SalesChartDataPointDto>
        {
            Items = [expectedDto],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mapper.Map<GetSalesChartDataQuery>(request).Returns(query);
        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<SalesChartDataPointDto>>.Success(paginatedResult));

        var result = await _controller.GetSalesChartData(request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<SalesChartDataPointDto>>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Items.ShouldHaveSingleItem();
        body.Data.Items[0].Revenue.ShouldBe(5000);
    }

    [Fact]
    public async Task GetTopSellingProducts_MapsRequestToQuery_AndReturnsOk()
    {
        var request = new GetTopSellingProductsRequest(5, null, null);
        var query = new GetTopSellingProductsQuery(5, null, null);

        var expectedDto = new TopSellingProductDto { ProductName = "Test Product", TotalQuantitySold = 100 };
        var paginatedResult = new PaginatedResult<TopSellingProductDto>
        {
            Items = [expectedDto],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mapper.Map<GetTopSellingProductsQuery>(request).Returns(query);
        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<TopSellingProductDto>>.Success(paginatedResult));

        var result = await _controller.GetTopSellingProducts(request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<TopSellingProductDto>>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Items.ShouldHaveSingleItem();
        body.Data.Items[0].ProductName.ShouldBe("Test Product");
    }

    [Fact]
    public async Task GetCategoryPerformance_MapsRequestToQuery_AndReturnsOk()
    {
        var request = new GetCategoryPerformanceRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = new GetCategoryPerformanceQuery(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var expectedDto = new CategoryPerformanceDto { CategoryName = "Electronics", TotalRevenue = 25000 };
        var paginatedResult = new PaginatedResult<CategoryPerformanceDto>
        {
            Items = [expectedDto],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mapper.Map<GetCategoryPerformanceQuery>(request).Returns(query);
        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<CategoryPerformanceDto>>.Success(paginatedResult));

        var result = await _controller.GetCategoryPerformance(request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<CategoryPerformanceDto>>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Items.ShouldHaveSingleItem();
        body.Data.Items[0].CategoryName.ShouldBe("Electronics");
    }

    [Fact]
    public async Task GetRevenueReport_MapsRequestToQuery_AndReturnsOk()
    {
        var request = new GetRevenueReportRequest(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var query = new GetRevenueReportQuery(
            new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc));

        var expectedDto = new RevenueReportDto { NetRevenue = 100000, GrossProfit = 40000 };
        _mapper.Map<GetRevenueReportQuery>(request).Returns(query);
        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RevenueReportDto>.Success(expectedDto));

        var result = await _controller.GetRevenueReport(request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<RevenueReportDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.NetRevenue.ShouldBe(100000);
    }

    [Fact]
    public async Task GetInventoryReport_SendsQueryWithoutParameters_AndReturnsOk()
    {
        var expectedDto = new InventoryReportDto { TotalVariants = 500, OutOfStockVariants = 25 };
        _mediator.Send(Arg.Any<GetInventoryReportQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<InventoryReportDto>.Success(expectedDto));

        var result = await _controller.GetInventoryReport(CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<InventoryReportDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.TotalVariants.ShouldBe(500);
        await _mediator.Received(1).Send(Arg.Any<GetInventoryReportQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminAnalyticsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminAnalyticsController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminAnalyticsController_HasApiControllerAttribute()
    {
        typeof(AdminAnalyticsController).GetCustomAttributes(typeof(ApiControllerAttribute), true)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AdminAnalyticsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminAnalyticsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/analytics");
    }

    [Fact]
    public async Task GetDashboardStatistics_WhenNotFound_MapsToNotFound()
    {
        var request = new GetDashboardStatisticsRequest(null, null);
        var query = new GetDashboardStatisticsQuery(null, null);

        _mapper.Map<GetDashboardStatisticsQuery>(request).Returns(query);
        _mediator.Send(query, Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DashboardStatisticsDto>.NotFound());

        var result = await _controller.GetDashboardStatistics(request, CancellationToken.None);

        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        var body = notFound.Value.ShouldBeOfType<ApiResponse<DashboardStatisticsDto>>();
        body.Success.ShouldBeFalse();
    }

    [Theory]
    [InlineData(nameof(AdminAnalyticsController.GetDashboardStatistics), "dashboard")]
    [InlineData(nameof(AdminAnalyticsController.GetSalesChartData), "sales-chart")]
    [InlineData(nameof(AdminAnalyticsController.GetTopSellingProducts), "top-products")]
    [InlineData(nameof(AdminAnalyticsController.GetCategoryPerformance), "category-performance")]
    [InlineData(nameof(AdminAnalyticsController.GetRevenueReport), "revenue")]
    [InlineData(nameof(AdminAnalyticsController.GetInventoryReport), "inventory")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(AdminAnalyticsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}