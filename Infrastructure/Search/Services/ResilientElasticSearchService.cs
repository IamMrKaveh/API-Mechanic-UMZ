using Application.Search.Contracts;
using Application.Search.Features.Queries.GetSearchIndexStats;
using Application.Search.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Infrastructure.Search.Services;

public sealed class ResilientElasticSearchService(
    ElasticsearchService inner,
    ElasticsearchCircuitBreaker circuitBreaker,
    IAuditService auditService) : ISearchService
{
    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams, CancellationToken ct = default)
        => circuitBreaker.ExecuteAsync<SearchResultDto<ProductSearchResultItemDto>>(
            () => inner.SearchProductsAsync(searchParams, ct));

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
        => circuitBreaker.ExecuteAsync<SearchResultDto<ProductSearchResultItemDto>>(
            () => inner.SearchWithFuzzyAsync(query, page, pageSize, ct));

    public Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
        => circuitBreaker.ExecuteAsync<GlobalSearchResultDto>(
            () => inner.SearchGlobalAsync(query, ct));

    public Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
        => circuitBreaker.ExecuteAsync<bool>(async () =>
        {
            await inner.IndexProductAsync(document, ct);
            return true;
        });

    public Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
        => circuitBreaker.ExecuteAsync<bool>(async () =>
        {
            await inner.IndexCategoryAsync(document, ct);
            return true;
        });

    public Task IndexBrandAsync(BrandSearchDocument document, CancellationToken ct = default)
        => circuitBreaker.ExecuteAsync<bool>(async () =>
        {
            await inner.IndexBrandAsync(document, ct);
            return true;
        });

    public Task<SearchIndexStatsDto> GetIndexStatsAsync(CancellationToken ct = default)
        => circuitBreaker.ExecuteAsync<SearchIndexStatsDto>(
            () => inner.GetIndexStatsAsync(ct));
}