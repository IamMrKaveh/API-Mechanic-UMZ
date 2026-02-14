namespace Application.Search.Features.Queries.SearchProducts;

public class SearchProductsHandler
    : IRequestHandler<SearchProductsQuery, ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>
{
    private readonly ISearchService _searchService;

    public SearchProductsHandler(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>> Handle(
        SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var searchParams = new SearchProductsParams
        {
            Q = request.Q ?? string.Empty,
            CategoryId = request.CategoryId,
            CategoryGroupId = request.CategoryGroupId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Brand = request.Brand,
            InStockOnly = request.InStockOnly,
            SortBy = request.SortBy,
            Page = request.Page,
            PageSize = request.PageSize,
            Tags = request.Tags
        };

        var result = await _searchService.SearchProductsAsync(searchParams, cancellationToken);

        return ServiceResult<SearchResultDto<ProductSearchResultItemDto>>.Success(result);
    }
}