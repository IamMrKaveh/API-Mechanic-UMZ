using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Application.Search.Features.Queries.GlobalSearch;
using Application.Search.Features.Queries.SearchProducts;
using MapsterMapper;
using Presentation.Search.Requests;

namespace Presentation.Search.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class SearchController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("products")]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] SearchProductsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<SearchProductsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("global")]
    public async Task<IActionResult> SearchGlobal(
        [FromQuery] string q,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GlobalSearchQuery(q), ct);
        return ToActionResult(result);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] GetSearchSuggestionsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetSearchSuggestionsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("products/fuzzy")]
    public async Task<IActionResult> SearchWithFuzzy(
        [FromQuery] FuzzySearchRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<FuzzySearchQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}