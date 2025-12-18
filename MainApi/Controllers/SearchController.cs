namespace MainApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        ISearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet("products")]
    public async Task<ActionResult<SearchResultDto<ProductSearchDocument>>> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] int? categoryId,
        [FromQuery] int? categoryGroupId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? brand,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string[]? tags = null,
        CancellationToken ct = default)
    {
        try
        {
            if (pageSize > 100)
            {
                return BadRequest(new { error = "حداکثر تعداد نتایج در هر صفحه 100 عدد است" });
            }

            var query = new SearchProductsQuery
            {
                Q = q ?? "",
                CategoryId = categoryId,
                CategoryGroupId = categoryGroupId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Brand = brand,
                InStockOnly = inStockOnly,
                SortBy = sortBy,
                Page = page,
                PageSize = pageSize,
                Tags = tags?.ToList()
            };

            var result = await _searchService.SearchProductsAsync(query, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new { error = "خطا در جستجوی محصولات" });
        }
    }

    [HttpGet("global")]
    public async Task<ActionResult<GlobalSearchResultDto>> SearchGlobal(
        [FromQuery] string q,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { error = "عبارت جستجو نمی‌تواند خالی باشد" });
            }

            var result = await _searchService.SearchGlobalAsync(q, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in global search");
            return StatusCode(500, new { error = "خطا در جستجوی سراسری" });
        }
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<List<string>>> GetSuggestions(
        [FromQuery] string q,
        [FromQuery] int maxSuggestions = 10,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { error = "عبارت جستجو نمی‌تواند خالی باشد" });
            }

            if (q.Length < 2)
            {
                return Ok(new List<string>());
            }

            var suggestions = await ((ElasticSearchService)_searchService).GetSuggestionsAsync(q, maxSuggestions, ct);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return StatusCode(500, new { error = "خطا در دریافت پیشنهادات" });
        }
    }

    [HttpGet("products/fuzzy")]
    public async Task<ActionResult<SearchResultDto<ProductSearchDocument>>> SearchWithFuzzy(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { error = "عبارت جستجو نمی‌تواند خالی باشد" });
            }

            var result = await ((ElasticSearchService)_searchService).SearchWithFuzzyAsync(q, page, pageSize, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fuzzy search");
            return StatusCode(500, new { error = "خطا در جستجوی fuzzy" });
        }
    }

    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "ElasticSearch"
        });
    }
}