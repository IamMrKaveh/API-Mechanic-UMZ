namespace Infrastructure.Search.Services;

public class ElasticSearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticSearchService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, float> _fieldBoosts;

    public ElasticSearchService(
        ElasticsearchClient client,
        ILogger<ElasticSearchService> logger,
        IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;

        _fieldBoosts = configuration
            .GetSection("Search:FieldBoosts")
            .Get<Dictionary<string, float>>() ?? new Dictionary<string, float>
            {
                { "name", 5 },
                { "categoryName", 3 },
                { "categoryGroupName", 2 },
                { "description", 1 },
                { "brand", 2.5f },
                { "tags", 2 }
            };
    }

    public async Task<SearchResultDto<ProductSearchResultItemDto>> SearchProductsAsync(
        SearchProductsParams searchParams,
        CancellationToken ct = default)
    {
        var shouldQueries = new List<Query>();
        var filterQueries = new List<Query>();

        if (!string.IsNullOrWhiteSpace(searchParams.Q))
        {
            shouldQueries.Add(new MatchQuery
            {
                Field = "name.keyword",
                Query = searchParams.Q,
                Boost = 10
            });

            shouldQueries.Add(new MatchPhraseQuery
            {
                Field = "name",
                Query = searchParams.Q,
                Boost = 7,
                Slop = 2
            });

            shouldQueries.Add(new MultiMatchQuery
            {
                Query = searchParams.Q,
                Fields = new[]
                {
                    $"name^{_fieldBoosts["name"]}",
                    $"categoryName^{_fieldBoosts["categoryName"]}",
                    $"categoryGroupName^{_fieldBoosts["categoryGroupName"]}",
                    $"description^{_fieldBoosts["description"]}",
                    $"brand^{_fieldBoosts["brand"]}",
                    $"tags^{_fieldBoosts["tags"]}"
                },
                Type = TextQueryType.BestFields,
                Fuzziness = "AUTO",
                PrefixLength = 2,
                MaxExpansions = 50
            });

            if (searchParams.Q.Length >= 3)
            {
                shouldQueries.Add(new WildcardQuery
                {
                    Field = "name",
                    Value = $"*{searchParams.Q}*",
                    CaseInsensitive = true,
                    Boost = 2
                });
            }

            shouldQueries.Add(new MultiMatchQuery
            {
                Query = searchParams.Q,
                Fields = new[] { "name", "categoryName", "brand" },
                Type = TextQueryType.CrossFields,
                Operator = Operator.And,
                Boost = 3
            });
        }

        filterQueries.Add(new TermQuery { Field = "isActive", Value = true });

        if (searchParams.CategoryId.HasValue)
            filterQueries.Add(new TermQuery { Field = "categoryId", Value = searchParams.CategoryId.Value });

        if (searchParams.CategoryGroupId.HasValue)
            filterQueries.Add(new TermQuery { Field = "categoryGroupId", Value = searchParams.CategoryGroupId.Value });

        if (searchParams.MinPrice.HasValue)
            filterQueries.Add(new NumberRangeQuery { Field = "price", Gte = (double)searchParams.MinPrice.Value });

        if (searchParams.MaxPrice.HasValue)
            filterQueries.Add(new NumberRangeQuery { Field = "price", Lte = (double)searchParams.MaxPrice.Value });

        if (searchParams.InStockOnly)
            filterQueries.Add(new TermQuery { Field = "inStock", Value = true });

        if (!string.IsNullOrWhiteSpace(searchParams.Brand))
            filterQueries.Add(new TermQuery { Field = "brand.keyword", Value = searchParams.Brand });

        if (searchParams.Tags?.Any() == true)
        {
            filterQueries.Add(new TermsQuery
            {
                Field = "tags",
                Terms = new TermsQueryField(searchParams.Tags.Select(t => (FieldValue)t).ToArray())
            });
        }

        var request = new SearchRequest("products_v1")
        {
            From = (searchParams.Page - 1) * searchParams.PageSize,
            Size = searchParams.PageSize,
            MinScore = _configuration.GetValue("Search:MinScore", 0.3),
            Query = new BoolQuery
            {
                Should = shouldQueries,
                Filter = filterQueries,
                MinimumShouldMatch = shouldQueries.Any() ? 1 : null
            },
            Sort = new List<SortOptions> { BuildSortOptions(searchParams.SortBy) },
            Aggregations = BuildAggregations()
        };

        var response = await _client.SearchAsync<ProductSearchDocument>(request, ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(response.DebugInformation);

        return new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = response.Documents.Select(MapToProductSearchResultItem).ToList(),
            Total = response.Total,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        };
    }

    public async Task<SearchResultDto<ProductSearchResultItemDto>> SearchWithFuzzyAsync(
        string searchQuery,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var response = await _client.SearchAsync<ProductSearchDocument>(s => s
            .Indices("products_v1")
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q
                .Bool(b => b
                    .Should(
                        sh => sh.MultiMatch(m => m
                            .Query(searchQuery)
                            .Fields(new[]
                            {
                                $"name^{_fieldBoosts["name"]}",
                                $"categoryName^{_fieldBoosts["categoryName"]}",
                                $"categoryGroupName^{_fieldBoosts["categoryGroupName"]}",
                                $"description^{_fieldBoosts["description"]}",
                                $"brand^{_fieldBoosts["brand"]}"
                            })
                            .Fuzziness(new Fuzziness("AUTO"))
                            .PrefixLength(1)
                            .MaxExpansions(50)
                        ),
                        sh => sh.Wildcard(w => w
                            .Field(new Field("name"))
                            .Value($"*{searchQuery}*")
                            .CaseInsensitive(true)
                        )
                    )
                    .Filter(f => f.Term(t => t.Field(new Field("isActive")).Value(FieldValue.Boolean(true))))
                    .MinimumShouldMatch(1)
                )
            ), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Fuzzy search failed: {Error}", response.DebugInformation);
            throw new InvalidOperationException("Fuzzy search operation failed");
        }

        return new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = response.Documents.Select(MapToProductSearchResultItem).ToList(),
            Total = response.Total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
    {
        var products = await SearchProductsInternalAsync(query, 10, ct);
        var categories = await SearchCategoriesInternalAsync(query, 5, ct);
        var categoryGroups = await SearchCategoryGroupsInternalAsync(query, 5, ct);

        return new GlobalSearchResultDto
        {
            Products = products,
            Categories = categories,
            CategoryGroups = categoryGroups,
            Query = query
        };
    }

    public async Task<List<string>> GetSuggestionsAsync(
        string query,
        int maxSuggestions = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<string>();

        try
        {
            var searchRequest = new SearchRequest("products_v1")
            {
                Size = 0,
                Suggest = new Suggester
                {
                    Suggesters = new Dictionary<string, FieldSuggester>
                    {
                        ["product_suggest"] = new FieldSuggester
                        {
                            Completion = new CompletionSuggester
                            {
                                Field = "name.suggest",
                                Size = maxSuggestions,
                                SkipDuplicates = true
                            },
                            Prefix = query
                        }
                    }
                }
            };

            var response = await _client.SearchAsync<ProductSearchDocument>(searchRequest, ct);

            if (!response.IsValidResponse || response.Suggest == null)
            {
                _logger.LogWarning("Suggest query failed or returned null");
                return new List<string>();
            }

            if (response.Suggest.TryGetValue("product_suggest", out var suggestions))
            {
                var results = new List<string>();

                foreach (var suggestion in suggestions)
                {
                    if (suggestion is CompletionSuggest<ProductSearchDocument> completionSuggest)
                    {
                        foreach (var option in completionSuggest.Options)
                        {
                            if (!string.IsNullOrWhiteSpace(option.Text))
                                results.Add(option.Text);
                        }
                    }
                }

                return results.Distinct().ToList();
            }

            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during get suggestions for query: {Query}", query);
            return new List<string>();
        }
    }

    public async Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
    {
        var response = await _client.IndexAsync(document, i => i
            .Index("products_v1")
            .Id(document.ProductId)
            .Refresh(Refresh.WaitFor), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to index product {ProductId}: {Error}", document.ProductId, response.DebugInformation);
            throw new InvalidOperationException($"Failed to index product {document.ProductId}");
        }

        _logger.LogInformation("Successfully indexed product {ProductId}", document.ProductId);
    }

    public async Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
    {
        var response = await _client.IndexAsync(document, i => i
            .Index("categories_v1")
            .Id(document.CategoryId)
            .Refresh(Refresh.WaitFor), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to index category {CategoryId}: {Error}", document.CategoryId, response.DebugInformation);
            throw new InvalidOperationException($"Failed to index category {document.CategoryId}");
        }

        _logger.LogInformation("Successfully indexed category {CategoryId}", document.CategoryId);
    }

    public async Task IndexCategoryGroupAsync(CategoryGroupSearchDocument document, CancellationToken ct = default)
    {
        var response = await _client.IndexAsync(document, i => i
            .Index("categorygroups_v1")
            .Id(document.CategoryGroupId)
            .Refresh(Refresh.WaitFor), ct);

        if (!response.IsValidResponse)
        {
            _logger.LogError("Failed to index category group {CategoryGroupId}: {Error}", document.CategoryGroupId, response.DebugInformation);
            throw new InvalidOperationException($"Failed to index category group {document.CategoryGroupId}");
        }

        _logger.LogInformation("Successfully indexed category group {CategoryGroupId}", document.CategoryGroupId);
    }

    // ========== Private Helpers ==========

    private async Task<List<ProductSearchResultItemDto>> SearchProductsInternalAsync(
        string query, int maxResults, CancellationToken ct)
    {
        var response = await _client.SearchAsync<ProductSearchDocument>(s => s
            .Indices("products_v1")
            .Size(maxResults)
            .Query(q => q
                .MultiMatch(m => m
                    .Query(query)
                    .Fields(new[] { "name^5", "categoryName^3", "description" })
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            ), ct);

        return response.IsValidResponse
            ? response.Documents.Select(MapToProductSearchResultItem).ToList()
            : new List<ProductSearchResultItemDto>();
    }

    private async Task<List<CategorySearchSummaryDto>> SearchCategoriesInternalAsync(
        string query, int maxResults, CancellationToken ct)
    {
        var response = await _client.SearchAsync<CategorySearchDocument>(s => s
            .Indices("categories_v1")
            .Size(maxResults)
            .Query(q => q
                .Match(m => m
                    .Field(f => f.Name)
                    .Query(query)
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            ), ct);

        if (!response.IsValidResponse)
            return new List<CategorySearchSummaryDto>();

        return response.Documents.Select(d => new CategorySearchSummaryDto
        {
            Id = d.CategoryId,
            Name = d.Name,
            Slug = d.Slug,
            IsActive = d.IsActive,
            ProductCount = d.ProductCount
        }).ToList();
    }

    private async Task<List<CategoryGroupSearchSummaryDto>> SearchCategoryGroupsInternalAsync(
        string query, int maxResults, CancellationToken ct)
    {
        var response = await _client.SearchAsync<CategoryGroupSearchDocument>(s => s
            .Indices("categorygroups_v1")
            .Size(maxResults)
            .Query(q => q
                .Match(m => m
                    .Field(f => f.Name)
                    .Query(query)
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            ), ct);

        if (!response.IsValidResponse)
            return new List<CategoryGroupSearchSummaryDto>();

        return response.Documents.Select(d => new CategoryGroupSearchSummaryDto
        {
            Id = d.CategoryGroupId,
            Name = d.Name,
            Slug = d.Slug,
            CategoryId = d.CategoryId,
            CategoryName = d.CategoryName,
            IsActive = d.IsActive,
            ProductCount = d.ProductCount
        }).ToList();
    }

    private static ProductSearchResultItemDto MapToProductSearchResultItem(ProductSearchDocument doc)
    {
        return new ProductSearchResultItemDto
        {
            ProductId = doc.ProductId,
            Name = doc.Name,
            Description = doc.Description,
            Slug = doc.Slug,
            Sku = doc.Sku,
            CategoryName = doc.CategoryName,
            CategoryId = doc.CategoryId,
            CategoryGroupName = doc.CategoryGroupName,
            CategoryGroupId = doc.CategoryGroupId,
            Price = doc.Price,
            DiscountedPrice = doc.DiscountedPrice.HasValue ? (decimal)doc.DiscountedPrice.Value : null,
            DiscountPercentage = doc.DiscountPercentage.HasValue ? (decimal)doc.DiscountPercentage.Value : null,
            ImageUrl = doc.ImageUrl,
            Images = doc.Images ?? new List<string>(),
            IsActive = doc.IsActive,
            InStock = doc.InStock,
            StockQuantity = doc.StockQuantity,
            AverageRating = (decimal)doc.AverageRating,
            ReviewCount = doc.ReviewCount,
            SalesCount = doc.SalesCount,
            CreatedAt = doc.CreatedAt
        };
    }

    private static Dictionary<string, Aggregation> BuildAggregations()
    {
        return new()
        {
            ["categories"] = new TermsAggregation { Field = "categoryId", Size = 50 },
            ["brands"] = new TermsAggregation { Field = "brand.keyword", Size = 50 },
            ["tags"] = new TermsAggregation { Field = "tags", Size = 30 },
            ["priceRanges"] = new RangeAggregation
            {
                Field = "price",
                Ranges = new List<AggregationRange>
                {
                    new() { To = 100000 },
                    new() { From = 100000, To = 500000 },
                    new() { From = 500000, To = 1000000 },
                    new() { From = 1000000 }
                }
            }
        };
    }

    private static SortOptions BuildSortOptions(string? sortBy)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "price_asc" => new SortOptions { Field = new FieldSort { Field = "price", Order = SortOrder.Asc } },
            "price_desc" => new SortOptions { Field = new FieldSort { Field = "price", Order = SortOrder.Desc } },
            "name_asc" => new SortOptions { Field = new FieldSort { Field = "name.keyword", Order = SortOrder.Asc } },
            "name_desc" => new SortOptions { Field = new FieldSort { Field = "name.keyword", Order = SortOrder.Desc } },
            "newest" => new SortOptions { Field = new FieldSort { Field = "createdAt", Order = SortOrder.Desc } },
            "rating" => new SortOptions { Field = new FieldSort { Field = "averageRating", Order = SortOrder.Desc } },
            "popular" => new SortOptions { Field = new FieldSort { Field = "salesCount", Order = SortOrder.Desc } },
            _ => new SortOptions { Score = new ScoreSort { Order = SortOrder.Desc } }
        };
    }
}