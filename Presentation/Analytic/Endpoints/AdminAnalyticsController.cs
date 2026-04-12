using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Queries.GetInventoryReport;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using MapsterMapper;
using Presentation.Analytic.Requests;

namespace Presentation.Analytic.Endpoints;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStatistics(
        [FromQuery] GetDashboardStatisticsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetDashboardStatisticsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChartData(
        [FromQuery] GetSalesChartDataRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetSalesChartDataQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopSellingProducts(
        [FromQuery] GetTopSellingProductsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetTopSellingProductsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("category-performance")]
    public async Task<IActionResult> GetCategoryPerformance(
        [FromQuery] GetCategoryPerformanceRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetCategoryPerformanceQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] GetRevenueReportRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetRevenueReportQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetInventoryReportQuery(), ct);
        return ToActionResult(result);
    }
}