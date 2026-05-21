using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Queries.GetInventoryReport;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using Application.Analytics.Features.Shared;
using Presentation.Analytic.Requests;

namespace Presentation.Analytic.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<DashboardStatisticsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStatistics(
        [FromQuery] GetDashboardStatisticsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetDashboardStatisticsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("sales-chart")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<SalesChartDataPointDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesChartData(
        [FromQuery] GetSalesChartDataRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetSalesChartDataQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("top-products")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<TopSellingProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopSellingProducts(
        [FromQuery] GetTopSellingProductsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetTopSellingProductsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("category-performance")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CategoryPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryPerformance(
        [FromQuery] GetCategoryPerformanceRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetCategoryPerformanceQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("revenue")]
    [ProducesResponseType(typeof(ApiResponse<RevenueReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] GetRevenueReportRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetRevenueReportQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("inventory")]
    [ProducesResponseType(typeof(ApiResponse<InventoryReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryReport(CancellationToken ct)
    {
        var query = new GetInventoryReportQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}