namespace MainApi.Search.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
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
        var query = new SearchProductsQuery
        {
            Q = q,
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

        var result = await _mediator.Send(query, ct);

        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("global")]
    public async Task<IActionResult> SearchGlobal(
        [FromQuery] string q,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GlobalSearchQuery(q), ct);

        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string q,
        [FromQuery] int maxSuggestions = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetSearchSuggestionsQuery(q, maxSuggestions), ct);

        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("products/fuzzy")]
    public async Task<IActionResult> SearchWithFuzzy(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new FuzzySearchQuery(q, page, pageSize), ct);

        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, new { error = result.Error });

        return Ok(result.Data);
    }
}