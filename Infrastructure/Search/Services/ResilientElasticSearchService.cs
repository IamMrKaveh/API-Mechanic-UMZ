using Application.Audit.Contracts;
using Application.Search.Contracts;
using Application.Search.Features.Queries.GetSearchIndexStats;
using Application.Search.Features.Shared;

namespace Infrastructure.Search.Services;

public sealed class ResilientElasticSearchService(
    ElasticsearchService innerService,
    ElasticsearchCircuitBreaker circuitBreaker,
    IAuditService auditService) : ISearchService
{
    public async Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
    {
        if (!circuitBreaker.IsAllowed())
        {
            await auditService.LogWarningAsync("Circuit breaker open — skipping product indexing", ct);
            return;
        }

        try
        {
            await innerService.IndexProductAsync(document, ct);
            circuitBreaker.RecordSuccess();
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            await auditService.LogErrorAsync($"IndexProductAsync failed: {ex.Message}", ct);
            throw;
        }
    }

    public async Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
    {
        if (!circuitBreaker.IsAllowed()) return;
        try
        {
            await innerService.IndexCategoryAsync(document, ct);
            circuitBreaker.RecordSuccess();
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            await auditService.LogErrorAsync($"IndexCategoryAsync failed: {ex.Message}", ct);
            throw;
        }
    }

    public async Task IndexBrandAsync(BrandSearchDocument document, CancellationToken ct = default)
    {
        if (!circuitBreaker.IsAllowed()) return;
        try
        {
            await innerService.IndexBrandAsync(document, ct);
            circuitBreaker.RecordSuccess();
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            await auditService.LogErrorAsync($"IndexBrandAsync failed: {ex.Message}", ct);
            throw;
        }
    }

    public Task DeleteProductAsync(Guid productId, CancellationToken ct = default)
        => innerService.DeleteProductAsync(productId, ct);

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams, CancellationToken ct = default)
        => innerService.SearchProductsAsync(searchParams, ct);

    public async Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
    {
        if (!circuitBreaker.IsAllowed())
            return new GlobalSearchResultDto { Query = query };

        try
        {
            var result = await innerService.SearchGlobalAsync(query, ct);
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            await auditService.LogErrorAsync($"SearchGlobalAsync failed: {ex.Message}", ct);
            return new GlobalSearchResultDto { Query = query };
        }
    }

    public async Task<List<string>> GetSuggestionsAsync(
        string query, int maxSuggestions = 10, CancellationToken ct = default)
    {
        if (!circuitBreaker.IsAllowed())
            return [];

        try
        {
            var result = await innerService.GetSuggestionsAsync(query, maxSuggestions, ct);
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            await auditService.LogErrorAsync($"GetSuggestionsAsync failed: {ex.Message}", ct);
            return [];
        }
    }

    public async Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string searchQuery, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (!circuitBreaker.IsAllowed())
            return new SearchResultDto<ProductSearchResultItemDto>();

        try
        {
            var result = await innerService.SearchWithFuzzyAsync(searchQuery, page, pageSize, ct);
            circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.RecordFailure();
            await auditService.LogErrorAsync($"SearchWithFuzzyAsync failed: {ex.Message}", ct);
            return new SearchResultDto<ProductSearchResultItemDto>();
        }
    }

    public Task<SearchIndexStatsDto?> GetIndexStatsAsync(CancellationToken ct = default)
        => innerService.GetIndexStatsAsync(ct);
}