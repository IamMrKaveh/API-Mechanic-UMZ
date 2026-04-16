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

    public Task<SearchIndexStatsDto?> GetIndexStatsAsync(CancellationToken ct = default)
        => innerService.GetIndexStatsAsync(ct);

    public Task<IEnumerable<string>> GetSuggestionsAsync(
        string query, int size = 5, CancellationToken ct = default)
        => innerService.GetSuggestionsAsync(query, size, ct);
}