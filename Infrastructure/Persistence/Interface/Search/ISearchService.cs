namespace Infrastructure.Persistence.Interface.Search;

public interface ISearchService
{
    Task<SearchResultDto<ProductSummaryDto>> SearchProductsAsync(SearchProductsQuery query, CancellationToken ct = default);
    Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default);
    Task ReindexProductAsync(int productId, CancellationToken ct = default);
    Task ReindexCategoryAsync(int categoryId, CancellationToken ct = default);
    Task ReindexCategoryGroupAsync(int groupId, CancellationToken ct = default);
}