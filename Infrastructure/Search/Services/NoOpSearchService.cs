using Application.Search.Features.Queries.GetSearchIndexStats;
using Application.Search.Features.Shared;

namespace Infrastructure.Search.Services;

public sealed class NoOpSearchService : ISearchService
{
    public Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task IndexBrandAsync(BrandSearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams, CancellationToken ct = default)
        => Task.FromResult(new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = [],
            Total = 0,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        });

    public Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
        => Task.FromResult(new GlobalSearchResultDto
        {
            Query = query,
            Products = []
        });

    public Task<List<string>> GetSuggestionsAsync(
        string query,
        int maxSuggestions = 10,
        CancellationToken ct = default)
        => Task.FromResult(new List<string>());

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string searchQuery,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
        => Task.FromResult(new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = [],
            Total = 0,
            Page = page,
            PageSize = pageSize
        });

    public Task<SearchIndexStatsDto?> GetIndexStatsAsync(CancellationToken ct = default)
        => Task.FromResult<SearchIndexStatsDto?>(new SearchIndexStatsDto(0, 0, 0, 0));
}