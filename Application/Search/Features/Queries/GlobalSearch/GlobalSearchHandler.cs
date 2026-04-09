using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.GlobalSearch;

public class GlobalSearchHandler(ISearchService searchService) : IRequestHandler<GlobalSearchQuery, ServiceResult<GlobalSearchResultDto>>
{
    private readonly ISearchService _searchService = searchService;

    public async Task<ServiceResult<GlobalSearchResultDto>> Handle(
        GlobalSearchQuery request, CancellationToken ct)
    {
        var result = await _searchService.SearchGlobalAsync(request.Q, ct);

        return ServiceResult<GlobalSearchResultDto>.Success(result);
    }
}