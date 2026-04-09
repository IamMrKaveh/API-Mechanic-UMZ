using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Application.Search.Features.Queries.GlobalSearch;
using Application.Search.Features.Queries.SearchProducts;

namespace Presentation.Search.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class SearchController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? BrandId,
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
        var query = new SearchProductsQuery(
            q,
            categoryId,
            BrandId,
            minPrice,
            maxPrice,
            brand,
            inStockOnly,
            sortBy,
            tags?.ToList(),
            page,
            pageSize);
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
        var query = new FuzzySearchQuery(q, page, pageSize);
        var result = await _mediator.Send(
            query,
            ct);
        return ToActionResult(result);
    }
}