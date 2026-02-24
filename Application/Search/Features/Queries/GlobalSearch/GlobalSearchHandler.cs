namespace Application.Search.Features.Queries.GlobalSearch;

public class GlobalSearchHandler
    : IRequestHandler<GlobalSearchQuery, ServiceResult<GlobalSearchResultDto>>
{
    private readonly ISearchService _searchService;

    public GlobalSearchHandler(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<ServiceResult<GlobalSearchResultDto>> Handle(
        GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        var result = await _searchService.SearchGlobalAsync(request.Q, cancellationToken);

        return ServiceResult<GlobalSearchResultDto>.Success(result);
    }
}