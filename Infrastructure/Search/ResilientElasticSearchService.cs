using Application.Common.Interfaces.Search;
using Application.DTOs.Search;

namespace Infrastructure.Search;

public class ResilientElasticSearchService : ISearchService
{
    private readonly ElasticSearchService _innerService;
    private readonly ILogger<ResilientElasticSearchService> _logger;

    public ResilientElasticSearchService(ElasticSearchService innerService, ILogger<ResilientElasticSearchService> logger)
    {
        _innerService = innerService;
        _logger = logger;
    }

    public Task<SearchResultDto<ProductSearchDocument>> SearchProductsAsync(SearchProductsQuery query, CancellationToken ct = default)
        => _innerService.SearchProductsAsync(query, ct);

    public Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
        => _innerService.SearchGlobalAsync(query, ct);

    public Task IndexProductAsync(ProductSearchDocument doc, CancellationToken ct = default) => _innerService.IndexProductAsync(doc, ct);
    public Task IndexCategoryAsync(CategorySearchDocument doc, CancellationToken ct = default) => _innerService.IndexCategoryAsync(doc, ct);
    public Task IndexCategoryGroupAsync(CategoryGroupSearchDocument doc, CancellationToken ct = default) => _innerService.IndexCategoryGroupAsync(doc, ct);
}