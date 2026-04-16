using Application.Audit.Contracts;
using Application.Search.Contracts;
using Application.Search.Features.Queries.GetSearchIndexStats;
using Application.Search.Features.Shared;

namespace Infrastructure.Search.Services;

public sealed class NoOpSearchService(IAuditService auditService) : ISearchService
{
    public Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task IndexBrandAsync(BrandSearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteProductAsync(Guid productId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams, CancellationToken ct = default)
        => Task.FromResult(new SearchResultDto<ProductSearchResultItemDto>());

    public Task<SearchIndexStatsDto?> GetIndexStatsAsync(CancellationToken ct = default)
        => Task.FromResult<SearchIndexStatsDto?>(new SearchIndexStatsDto(0, 0, 0, 0));

    public Task<IEnumerable<string>> GetSuggestionsAsync(
        string query, int size = 5, CancellationToken ct = default)
        => Task.FromResult<IEnumerable<string>>([]);

    public Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    Task<List<string>> ISearchService.GetSuggestionsAsync(string query, int maxSuggestions, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(string searchQuery, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}