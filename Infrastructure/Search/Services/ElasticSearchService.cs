using Application.Search.Features.Queries.GetSearchIndexStats;
using Application.Search.Features.Shared;
using Infrastructure.Search.Options;

namespace Infrastructure.Search.Services;

public sealed class ElasticsearchService(
    ElasticsearchClient client,
    IOptions<ElasticsearchOptions> options,
    IAuditService auditService) : ISearchService
{
    public async Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(document, i => i
            .Index("products_v1")
            .Id(document.ProductId), ct);

        if (!response.IsValidResponse)
            await auditService.LogErrorAsync(
                $"Failed to index product {document.ProductId}: {response.DebugInformation}", ct);
    }

    public async Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(document, i => i
            .Index("categories_v1")
            .Id(document.CategoryId), ct);

        if (!response.IsValidResponse)
            await auditService.LogErrorAsync(
                $"Failed to index category {document.CategoryId}: {response.DebugInformation}", ct);
    }

    public async Task IndexBrandAsync(BrandSearchDocument document, CancellationToken ct = default)
    {
        var response = await client.IndexAsync(document, i => i
            .Index("brands_v1")
            .Id(document.BrandId), ct);

        if (!response.IsValidResponse)
            await auditService.LogErrorAsync(
                $"Failed to index brand {document.BrandId}: {response.DebugInformation}", ct);
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken ct = default)
    {
        await client.DeleteAsync("products_v1", productId, ct);
    }

    public async Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams, CancellationToken ct = default)
    {
        var response = await client.SearchAsync<ProductSearchResultItemDto>(s => s
            .Index("products_v1")
            .From((searchParams.Page - 1) * searchParams.PageSize)
            .Size(searchParams.PageSize)
            .Query(q => q.Bool(b =>
            {
                if (!string.IsNullOrWhiteSpace(searchParams.Q))
                    b.Must(m => m.MultiMatch(mm => mm
                        .Query(searchParams.Q)
                        .Fields(["name^3", "description"])));
                return b;
            })), ct);

        if (!response.IsValidResponse)
            return new SearchResultDto<ProductSearchResultItemDto>();

        return new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = response.Documents.ToList(),
            Total = response.Total,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        };
    }

    public async Task<SearchIndexStatsDto?> GetIndexStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var statsResponse = await client.Indices.StatsAsync(ct: ct);
            if (!statsResponse.IsValidResponse) return null;

            return new SearchIndexStatsDto(
                ProductsCount: 0,
                CategoriesCount: 0,
                BrandsCount: 0,
                TotalDocuments: statsResponse.All.Total.Docs?.Count ?? 0);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<string>> GetSuggestionsAsync(
        string query, int size = 5, CancellationToken ct = default)
    {
        var response = await client.SearchAsync<ProductSearchDocument>(s => s
            .Index("products_v1")
            .Size(size)
            .Query(q => q.Prefix(p => p
                .Field(f => f.Name)
                .Value(query))), ct);

        if (!response.IsValidResponse)
            return [];

        return response.Documents.Select(d => d.Name).Take(size);
    }

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