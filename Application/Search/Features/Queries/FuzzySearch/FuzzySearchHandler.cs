using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.FuzzySearch;

public class FuzzySearchHandler(ISearchService searchService)
        : IRequestHandler<FuzzySearchQuery, ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>
{
    private readonly ISearchService _searchService = searchService;

    public async Task<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>> Handle(
        FuzzySearchQuery request, CancellationToken ct)
    {
        var result = await _searchService.SearchWithFuzzyAsync(
            request.Q, request.Page, request.PageSize, ct);

        return ServiceResult<SearchResultDto<ProductSearchResultItemDto>>.Success(result);
    }
}