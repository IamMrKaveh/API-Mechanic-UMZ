using Presentation.Base.Controllers.v1;

namespace Presentation.Search.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] int? categoryId,
        [FromQuery] int? BrandId,
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
            BrandId = BrandId,
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
        return ToActionResult(result);
    }

    [HttpGet("global")]
    public async Task<IActionResult> SearchGlobal(
        [FromQuery] string q,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GlobalSearchQuery(q), ct);
        return ToActionResult(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string q,
        [FromQuery] int maxSuggestions = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetSearchSuggestionsQuery(q, maxSuggestions), ct);
        return ToActionResult(result);
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
        return ToActionResult(result);
    }
}