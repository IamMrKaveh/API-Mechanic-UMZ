namespace MainApi.Analytic.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDashboardStatisticsQuery(fromDate, toDate), cancellationToken);
        return Ok(result);
    }

    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChartData(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string groupBy = "day",
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetSalesChartDataQuery(fromDate, toDate, groupBy), cancellationToken);
        return Ok(result);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopSellingProducts(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTopSellingProductsQuery(count, fromDate, toDate), cancellationToken);
        return Ok(result);
    }

    [HttpGet("category-performance")]
    public async Task<IActionResult> GetCategoryPerformance(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCategoryPerformanceQuery(fromDate, toDate), cancellationToken);
        return Ok(result);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueReport(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetRevenueReportQuery(fromDate, toDate), cancellationToken);
        return Ok(result);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport(
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetInventoryReportQuery(), cancellationToken);
        return Ok(result);
    }
}