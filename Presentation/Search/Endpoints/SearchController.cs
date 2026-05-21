using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Application.Search.Features.Queries.GlobalSearch;
using Application.Search.Features.Queries.SearchProducts;
using Application.Search.Features.Shared;
using Presentation.Search.Requests;

namespace Presentation.Search.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/search")]
public class SearchController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("products")]
    [ProducesResponseType(typeof(ApiResponse<SearchResultDto<ProductSearchResultItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] SearchProductsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<SearchProductsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("global")]
    [ProducesResponseType(typeof(ApiResponse<GlobalSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchGlobal(
        [FromQuery] string q,
        CancellationToken ct)
    {
        var query = new GlobalSearchQuery(q);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] GetSearchSuggestionsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetSearchSuggestionsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("products/fuzzy")]
    [ProducesResponseType(typeof(ApiResponse<SearchResultDto<ProductSearchResultItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchWithFuzzy(
        [FromQuery] FuzzySearchRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<FuzzySearchQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}