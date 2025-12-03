namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    public AdminAnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var statistics = await _analyticsService.GetDashboardStatisticsAsync(fromDate, toDate);
        return Ok(statistics);
    }

    [HttpGet("sales-chart")]
    public async Task<IActionResult> GetSalesChartData(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string groupBy = "day")
    {
        var chartData = await _analyticsService.GetSalesChartDataAsync(fromDate, toDate, groupBy);
        return Ok(chartData);
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopSellingProducts(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var topProducts = await _analyticsService.GetTopSellingProductsAsync(count, fromDate, toDate);
        return Ok(topProducts);
    }

    [HttpGet("category-performance")]
    public async Task<IActionResult> GetCategoryPerformance(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var performance = await _analyticsService.GetCategoryPerformanceAsync(fromDate, toDate);
        return Ok(performance);
    }
}