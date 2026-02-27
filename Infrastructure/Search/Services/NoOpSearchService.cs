namespace Infrastructure.Search.Services;

/// <summary>
/// سرویس جستجوی No-Op که زمانی استفاده می‌شود که Elasticsearch غیرفعال است
/// </summary>
public class NoOpSearchService : ISearchService
{
    private readonly ILogger<NoOpSearchService> _logger;

    public NoOpSearchService(ILogger<NoOpSearchService> logger)
    {
        _logger = logger;
    }

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams,
        CancellationToken ct = default)
    {
        _logger.LogWarning("Search is disabled. Returning empty results.");
        return Task.FromResult(new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = new List<ProductSearchResultItemDto>(),
            Total = 0,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        });
    }

    public Task<GlobalSearchResultDto> SearchGlobalAsync(
        string query,
        CancellationToken ct = default)
    {
        _logger.LogWarning("Global search is disabled. Returning empty results.");
        return Task.FromResult(new GlobalSearchResultDto
        {
            Products = new List<ProductSearchResultItemDto>(),
            Categories = new List<CategorySearchSummaryDto>(),
            Brands = new List<BrandSearchSummaryDto>(),
            Query = query
        });
    }

    public Task<List<string>> GetSuggestionsAsync(
        string query,
        int maxSuggestions = 10,
        CancellationToken ct = default)
    {
        _logger.LogWarning("Search suggestions are disabled. Returning empty list.");
        return Task.FromResult(new List<string>());
    }

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string searchQuery,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        _logger.LogWarning("Fuzzy search is disabled. Returning empty results.");
        return Task.FromResult(new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = new List<ProductSearchResultItemDto>(),
            Total = 0,
            Page = page,
            PageSize = pageSize
        });
    }

    public Task IndexProductAsync(
        ProductSearchDocument document,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Product indexing is disabled. Skipping index for Product {ProductId}",
            document.ProductId);
        return Task.CompletedTask;
    }

    public Task IndexCategoryAsync(
        CategorySearchDocument document,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Category indexing is disabled. Skipping index for Category {CategoryId}",
            document.CategoryId);
        return Task.CompletedTask;
    }

    public Task IndexBrandAsync(
        BrandSearchDocument document,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Brand indexing is disabled. Skipping index for Brand {BrandId}",
            document.BrandId);
        return Task.CompletedTask;
    }
}