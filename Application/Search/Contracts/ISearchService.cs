namespace Application.Search.Contracts;

/// <summary>
/// قرارداد سرویس جستجو - بدون وابستگی به Elasticsearch
/// </summary>
public interface ISearchService
{
    Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams,
        CancellationToken ct = default);

    Task<GlobalSearchResultDto> SearchGlobalAsync(
        string query,
        CancellationToken ct = default);

    Task<List<string>> GetSuggestionsAsync(
        string query,
        int maxSuggestions = 10,
        CancellationToken ct = default);

    Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string searchQuery,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default);

    Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default);

    Task IndexBrandAsync(BrandSearchDocument document, CancellationToken ct = default);
}