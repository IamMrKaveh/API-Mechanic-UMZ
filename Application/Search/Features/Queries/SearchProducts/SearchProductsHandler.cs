using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.SearchProducts;

public class SearchProductsHandler(ISearchService searchService)
        : IRequestHandler<SearchProductsQuery, ServiceResult<SearchResultDto<ProductSearchResultItemDto>>>
{
    public async Task<ServiceResult<SearchResultDto<ProductSearchResultItemDto>>> Handle(
        SearchProductsQuery request,
        CancellationToken ct)
    {
        var searchParams = new SearchProductsParams
        {
            Q = request.Q ?? string.Empty,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Brand = request.Brand,
            InStockOnly = request.InStockOnly,
            SortBy = request.SortBy,
            Page = request.Page,
            PageSize = request.PageSize,
            Tags = request.Tags
        };

        var result = await searchService.SearchProductsAsync(searchParams, ct);

        return ServiceResult<SearchResultDto<ProductSearchResultItemDto>>.Success(result);
    }
}