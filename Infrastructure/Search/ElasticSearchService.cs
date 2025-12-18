namespace Infrastructure.Search;

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

    public async Task<SearchResultDto<ProductSearchDocument>> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken ct = default)
    {
        var shouldQueries = new List<Query>();
        var filterQueries = new List<Query>();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            shouldQueries.Add(new MatchQuery
            {
                Field = "name.keyword",
                Query = query.Q,
                Boost = 10
            });

            shouldQueries.Add(new MatchPhraseQuery
            {
                Field = "name",
                Query = query.Q,
                Boost = 7,
                Slop = 2
            });

            shouldQueries.Add(new MultiMatchQuery
            {
                Query = query.Q,
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

            if (query.Q.Length >= 3)
            {
                shouldQueries.Add(new WildcardQuery
                {
                    Field = "name",
                    Value = $"*{query.Q}*",
                    CaseInsensitive = true,
                    Boost = 2
                });
            }

            shouldQueries.Add(new MultiMatchQuery
            {
                Query = query.Q,
                Fields = new[] { "name", "categoryName", "brand" },
                Type = TextQueryType.CrossFields,
                Operator = Operator.And,
                Boost = 3
            });
        }

        filterQueries.Add(new TermQuery
        {
            Field = "isActive",
            Value = true
        });

        if (query.CategoryId.HasValue)
        {
            filterQueries.Add(new TermQuery
            {
                Field = "categoryId",
                Value = query.CategoryId.Value
            });
        }

        if (query.CategoryGroupId.HasValue)
        {
            filterQueries.Add(new TermQuery
            {
                Field = "categoryGroupId",
                Value = query.CategoryGroupId.Value
            });
        }

        if (query.MinPrice.HasValue)
        {
            filterQueries.Add(new NumberRangeQuery
            {
                Field = "price",
                Gte = (double)query.MinPrice.Value
            });
        }

        if (query.MaxPrice.HasValue)
        {
            filterQueries.Add(new NumberRangeQuery
            {
                Field = "price",
                Lte = (double)query.MaxPrice.Value
            });
        }

        if (query.InStockOnly)
        {
            filterQueries.Add(new TermQuery
            {
                Field = "inStock",
                Value = true
            });
        }

        if (!string.IsNullOrWhiteSpace(query.Brand))
        {
            filterQueries.Add(new TermQuery
            {
                Field = "brand.keyword",
                Value = query.Brand
            });
        }

        if (query.Tags?.Any() == true)
        {
            filterQueries.Add(new TermsQuery
            {
                Field = "tags",
                Terms = new TermsQueryField(
                    query.Tags.Select(t => (FieldValue)t).ToArray()
                )
            });
        }

        var request = new SearchRequest("products_v1")
        {
            From = (query.Page - 1) * query.PageSize,
            Size = query.PageSize,
            MinScore = _configuration.GetValue<double>("Search:MinScore", 0.3),
            Query = new BoolQuery
            {
                Should = shouldQueries,
                Filter = filterQueries,
                MinimumShouldMatch = shouldQueries.Any() ? 1 : null
            },
            Sort = new List<SortOptions> { BuildSortOptions(query.SortBy) },
            Aggregations = BuildAggregations()
        };

        var response = await _client.SearchAsync<ProductSearchDocument>(request, ct);

        if (!response.IsValidResponse)
            throw new InvalidOperationException(response.DebugInformation);

        return new SearchResultDto<ProductSearchDocument>
        {
            Items = response.Documents.ToList(),
            Total = response.Total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<SearchResultDto<ProductSearchDocument>> SearchWithFuzzyAsync(
        string searchQuery,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
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

            return new SearchResultDto<ProductSearchDocument>
            {
                Items = response.Documents.ToList(),
                Total = response.Total,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during fuzzy search");
            throw;
        }
    }

    public async Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct = default)
    {
        try
        {
            var products = await SearchProductsInternalAsync(query, 10, ct);
            var categories = await SearchCategoriesAsync(query, 5, ct);
            var categoryGroups = await SearchCategoryGroupsAsync(query, 5, ct);

            return new GlobalSearchResultDto
            {
                Products = products,
                Categories = categories,
                CategoryGroups = categoryGroups
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during global search");
            throw;
        }
    }

    public async Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while indexing product {ProductId}", document.ProductId);
            throw;
        }
    }

    public async Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while indexing category {CategoryId}", document.CategoryId);
            throw;
        }
    }

    public async Task IndexCategoryGroupAsync(CategoryGroupSearchDocument document, CancellationToken ct = default)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while indexing category group {CategoryGroupId}", document.CategoryGroupId);
            throw;
        }
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
                            {
                                results.Add(option.Text);
                            }
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

    private async Task<List<ProductSearchDocument>> SearchProductsInternalAsync(string query, int maxResults, CancellationToken ct)
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

        return response.IsValidResponse ? response.Documents.ToList() : new List<ProductSearchDocument>();
    }

    private async Task<List<CategorySearchDocument>> SearchCategoriesAsync(string query, int maxResults, CancellationToken ct)
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

        return response.IsValidResponse ? response.Documents.ToList() : new List<CategorySearchDocument>();
    }

    private async Task<List<CategoryGroupSearchDocument>> SearchCategoryGroupsAsync(string query, int maxResults, CancellationToken ct)
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

        return response.IsValidResponse ? response.Documents.ToList() : new List<CategoryGroupSearchDocument>();
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
            "price_asc" => new SortOptions
            {
                Field = new FieldSort
                {
                    Field = "price",
                    Order = SortOrder.Asc
                }
            },

            "price_desc" => new SortOptions
            {
                Field = new FieldSort
                {
                    Field = "price",
                    Order = SortOrder.Desc
                }
            },

            "name_asc" => new SortOptions
            {
                Field = new FieldSort
                {
                    Field = "name.keyword",
                    Order = SortOrder.Asc
                }
            },

            "name_desc" => new SortOptions
            {
                Field = new FieldSort
                {
                    Field = "name.keyword",
                    Order = SortOrder.Desc
                }
            },

            "newest" => new SortOptions
            {
                Field = new FieldSort
                {
                    Field = "createdAt",
                    Order = SortOrder.Desc
                }
            },

            "rating" => new SortOptions
            {
                Field = new FieldSort
                {
                    Field = "averageRating",
                    Order = SortOrder.Desc
                }
            },

            "popular" => new SortOptions
            {
                Field = new FieldSort
                {
                    Field = "salesCount",
                    Order = SortOrder.Desc
                }
            },

            _ => new SortOptions
            {
                Score = new ScoreSort
                {
                    Order = SortOrder.Desc
                }
            }
        };
    }

    private Dictionary<string, List<string>> ExtractHighlights(SearchResponse<ProductSearchDocument> response)
    {
        var highlights = new Dictionary<string, List<string>>();

        foreach (var hit in response.Hits)
        {
            if (hit.Highlight != null)
            {
                foreach (var highlight in hit.Highlight)
                {
                    if (!highlights.ContainsKey(highlight.Key))
                    {
                        highlights[highlight.Key] = new List<string>();
                    }
                    highlights[highlight.Key].AddRange(highlight.Value);
                }
            }
        }

        return highlights;
    }

    private Dictionary<string, object> ExtractAggregations(SearchResponse<ProductSearchDocument> response)
    {
        var aggregations = new Dictionary<string, object>();

        if (response.Aggregations == null) return aggregations;

        foreach (var agg in response.Aggregations)
        {
            if (agg.Value is StringTermsAggregate termsAgg)
            {
                aggregations[agg.Key] = termsAgg.Buckets.Select(b => new
                {
                    Key = b.Key.ToString(),
                    Count = b.DocCount
                }).ToList();
            }
            else if (agg.Value is RangeAggregate rangeAgg)
            {
                aggregations[agg.Key] = rangeAgg.Buckets.Select(b => new
                {
                    From = b.From,
                    To = b.To,
                    Count = b.DocCount
                }).ToList();
            }
        }

        return aggregations;
    }
}