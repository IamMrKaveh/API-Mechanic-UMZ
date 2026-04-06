using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Queries.GetInventoryReport;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using Presentation.Base.Controllers.v1;

namespace Presentation.Analytic.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetDashboardStatisticsQuery(fromDate, toDate), ct);
        return ToActionResult(result);
    }

    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChartData(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string groupBy = "day",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetSalesChartDataQuery(fromDate, toDate, groupBy), ct);
        return ToActionResult(result);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopSellingProducts(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetTopSellingProductsQuery(count, fromDate, toDate), ct);
        return ToActionResult(result);
    }

    [HttpGet("category-performance")]
    public async Task<IActionResult> GetCategoryPerformance(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetCategoryPerformanceQuery(fromDate, toDate), ct);
        return ToActionResult(result);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetRevenueReportQuery(fromDate, toDate), ct);
        return ToActionResult(result);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport(
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetInventoryReportQuery(), ct);
        return ToActionResult(result);
    }
}