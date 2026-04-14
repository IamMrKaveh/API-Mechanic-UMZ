using Application.Search.Contracts;
using Application.Search.Features.Queries.GetSearchIndexStats;
using Application.Search.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Infrastructure.Search.Services;

public sealed class NoOpSearchService() : ISearchService
{
    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams, CancellationToken ct = default)
        => Task.FromResult(new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = [],
            Total = 0,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        });

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult(new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = [],
            Total = 0,
            Page = page,
            PageSize = pageSize
        });

    public Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
        => Task.FromResult(new GlobalSearchResultDto
        {
            Query = query,
            Products = [],
            Categories = [],
            Brands = []
        });

    public Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task IndexBrandAsync(BrandSearchDocument document, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<SearchIndexStatsDto> GetIndexStatsAsync(CancellationToken ct = default)
        => Task.FromResult(new SearchIndexStatsDto { IsAvailable = false });
}