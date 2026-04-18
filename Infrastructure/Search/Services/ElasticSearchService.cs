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
        var queries = new List<Query>();

        if (!string.IsNullOrWhiteSpace(searchParams.Q))
            queries.Add(new MultiMatchQuery
            {
                Fields = new[] { "name", "description", "categoryName", "brandName" },
                Query = searchParams.Q,
                Fuzziness = new Fuzziness("AUTO")
            });

        if (searchParams.CategoryId.HasValue)
            queries.Add(Term("categoryId", searchParams.CategoryId.Value));

        if (searchParams.BrandId.HasValue)
            queries.Add(Term("brandId", searchParams.BrandId.Value));

        if (searchParams.InStockOnly)
            queries.Add(Term("inStock", true));

        if (searchParams.MinPrice.HasValue || searchParams.MaxPrice.HasValue)
        {
            var rangeQuery = new NumberRangeQuery("price");
            if (searchParams.MinPrice.HasValue) rangeQuery.Gte = (double)searchParams.MinPrice.Value;
            if (searchParams.MaxPrice.HasValue) rangeQuery.Lte = (double)searchParams.MaxPrice.Value;
            queries.Add(rangeQuery);
        }

        var response = await client.SearchAsync<ProductSearchResultItemDto>(s => s
            .Indices("products_v1")
            .From((searchParams.Page - 1) * searchParams.PageSize)
            .Size(searchParams.PageSize)
            .Query(q =>
            {
                if (queries.Count == 0)
                    q.MatchAll(new MatchAllQuery());
                else
                    q.Bool(b => b.Must(queries.ToArray()));
            })
        , ct);

        if (!response.IsValidResponse)
        {
            await auditService.LogErrorAsync($"SearchProductsAsync failed: {response.DebugInformation}", ct);
            return new SearchResultDto<ProductSearchResultItemDto>();
        }

        return new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = response.Documents.ToList(),
            Total = response.Total,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        };
    }

    public async Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
    {
        var products = await SearchProductsAsync(new SearchProductsParams { Q = query, Page = 1, PageSize = 5 }, ct);

        return new GlobalSearchResultDto
        {
            Query = query,
            Products = products.Items
        };
    }

    public async Task<List<string>> GetSuggestionsAsync(
        string query, int maxSuggestions = 10, CancellationToken ct = default)
    {
        var response = await client.SearchAsync<ProductSearchDocument>(s => s
            .Indices("products_v1")
            .Size(maxSuggestions)
            .Query(q => q
                .MatchPhrasePrefix(mpp => mpp
                    .Field("name")
                    .Query(query)
                )
            )
        , ct);

        if (!response.IsValidResponse)
            return [];

        return response.Documents
            .Select(d => d.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .Take(maxSuggestions)
            .ToList();
    }

    public async Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string searchQuery, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await client.SearchAsync<ProductSearchResultItemDto>(s => s
            .Indices("products_v1")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .MultiMatch(mm => mm
                    .Fields(new[] { "name", "description", "brandName" })
                    .Query(searchQuery)
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            )
        , ct);

        if (!response.IsValidResponse)
        {
            await auditService.LogErrorAsync($"SearchWithFuzzyAsync failed: {response.DebugInformation}", ct);
            return new SearchResultDto<ProductSearchResultItemDto>();
        }

        return new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = response.Documents.ToList(),
            Total = response.Total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SearchIndexStatsDto?> GetIndexStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await client.Indices.StatsAsync(r => r
                .Indices(new[] { "products_v1", "categories_v1", "brands_v1" })
            , ct);

            if (!response.IsValidResponse)
                return null;

            var total = response.Indices?.Values.Sum(i => i.Total?.Docs?.Count ?? 0) ?? 0;
            return new SearchIndexStatsDto((int)total, 0, 0, 0);
        }
        catch
        {
            return null;
        }
    }

    private static TermQuery Term(string field, object value) => new()
    {
        Field = field,
        Value = value switch
        {
            Guid g => FieldValue.String(g.ToString()),
            bool b => FieldValue.Boolean(b),
            string s => FieldValue.String(s),
            int i => FieldValue.Long(i),
            double d => FieldValue.Double(d),
            _ => FieldValue.String(value.ToString()!)
        }
    };
}