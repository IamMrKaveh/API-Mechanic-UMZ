namespace Infrastructure.Search.Services;

public class ResilientElasticSearchService : ISearchService
{
    private readonly ElasticSearchService _innerService;
    private readonly ElasticsearchCircuitBreaker _circuitBreaker;
    private readonly ILogger<ResilientElasticSearchService> _logger;

    public ResilientElasticSearchService(
        ElasticSearchService innerService,
        ElasticsearchCircuitBreaker circuitBreaker,
        ILogger<ResilientElasticSearchService> logger)
    {
        _innerService = innerService;
        _circuitBreaker = circuitBreaker;
        _logger = logger;
    }

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams, CancellationToken ct = default)
    {
        return _circuitBreaker.ExecuteAsync(() =>
            _innerService.SearchProductsAsync(searchParams, ct));
    }

    public Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
    {
        return _circuitBreaker.ExecuteAsync(() =>
            _innerService.SearchGlobalAsync(query, ct));
    }

    public Task<List<string>> GetSuggestionsAsync(
        string query, int maxSuggestions = 10, CancellationToken ct = default)
    {
        return _circuitBreaker.ExecuteAsync(() =>
            _innerService.GetSuggestionsAsync(query, maxSuggestions, ct));
    }

    public Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string searchQuery, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return _circuitBreaker.ExecuteAsync(() =>
            _innerService.SearchWithFuzzyAsync(searchQuery, page, pageSize, ct));
    }

    public Task IndexProductAsync(ProductSearchDocument doc, CancellationToken ct = default)
        => _innerService.IndexProductAsync(doc, ct);

    public Task IndexCategoryAsync(CategorySearchDocument doc, CancellationToken ct = default)
        => _innerService.IndexCategoryAsync(doc, ct);

    public Task IndexCategoryGroupAsync(CategoryGroupSearchDocument doc, CancellationToken ct = default)
        => _innerService.IndexCategoryGroupAsync(doc, ct);
}