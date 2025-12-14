namespace Application.Common.Interfaces;

public interface ISearchService 
{ 
    Task<SearchResultDto<ProductSearchDocument>> SearchProductsAsync(SearchProductsQuery query, CancellationToken ct); 
    Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct); 
    Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct); 
    Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct); 
    Task IndexCategoryGroupAsync(CategoryGroupSearchDocument document, CancellationToken ct); 
}