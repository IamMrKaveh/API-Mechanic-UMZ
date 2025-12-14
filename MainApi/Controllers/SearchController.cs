namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchProducts([FromQuery] SearchProductsQuery query)
    {
        var result = await _searchService.SearchProductsAsync(query, HttpContext.RequestAborted);

        return Ok(new
        {
            items = result.Items,
            total = result.Total,
            page = result.Page,
            pageSize = result.PageSize,
            highlights = result.Highlights
        });
    }

    [HttpGet("global")]
    [AllowAnonymous]
    public async Task<IActionResult> GlobalSearch([FromQuery] string q)
    {
        var result = await _searchService.SearchGlobalAsync(q, HttpContext.RequestAborted);
        return Ok(result);
    }
}
