namespace Application.Search.Features.Queries.FuzzySearch;

public class FuzzySearchHandler
    : IRequestHandler<FuzzySearchQuery, ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>
{
    private readonly ISearchService _searchService;

    public FuzzySearchHandler(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>> Handle(
        FuzzySearchQuery request, CancellationToken cancellationToken)
    {
        var result = await _searchService.SearchWithFuzzyAsync(
            request.Q, request.Page, request.PageSize, cancellationToken);

        return ServiceResult<SearchResultDto<ProductSearchResultItemDto>>.Success(result);
    }
}