namespace Application.Common.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto<ProductSearchDocument>> SearchProductsAsync(SearchProductsQuery query, CancellationToken ct = default);
    Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default);
    Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default);
    Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default);
    Task IndexCategoryGroupAsync(CategoryGroupSearchDocument document, CancellationToken ct = default);
}