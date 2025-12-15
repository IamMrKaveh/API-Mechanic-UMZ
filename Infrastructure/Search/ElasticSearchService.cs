namespace Infrastructure.Search;

public class ElasticSearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticSearchService> _logger;
    private readonly ElasticsearchMetrics _metrics;

    public ElasticSearchService(
        ElasticsearchClient client,
        ILogger<ElasticSearchService> logger,
        ElasticsearchMetrics metrics)
    {
        _client = client;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<SearchResultDto<ProductSearchDocument>> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var searchRequest = new SearchRequest("products_v1")
            {
                From = (query.Page - 1) * query.PageSize,
                Size = query.PageSize,
                Query = BuildProductQuery(query),
                Sort = BuildSortOptions(query.Sort),
                Highlight = new Highlight
                {
                    Fields = new Dictionary<Field, HighlightField>
                    {
                        { "name", new HighlightField { PreTags = ["<em>"], PostTags = ["</em>"] } },
                        { "description", new HighlightField { PreTags = ["<em>"], PostTags = ["</em>"] } }
                    }
                }
            };

            var response = await _client.SearchAsync<ProductSearchDocument>(searchRequest, ct);
            stopwatch.Stop();

            var success = response.IsValidResponse;
            _metrics.RecordSearchRequest(stopwatch.Elapsed.TotalMilliseconds, success, "products");

            if (!success)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.DebugInformation);
                return new SearchResultDto<ProductSearchDocument>
                {
                    Items = [],
                    Total = 0,
                    Page = query.Page,
                    PageSize = query.PageSize
                };
            }

            return new SearchResultDto<ProductSearchDocument>
            {
                Items = response.Documents ?? [],
                Total = response.Total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordSearchRequest(stopwatch.Elapsed.TotalMilliseconds, false, "products");
            _logger.LogError(ex, "Error occurred while searching products with query: {Query}", query.Q);
            throw;
        }
    }

    public async Task<GlobalSearchResultDto> SearchGlobalAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new GlobalSearchResultDto();

        try
        {
            var categoryTask = SearchCategoriesAsync(query, ct);
            var categoryGroupTask = SearchCategoryGroupsAsync(query, ct);
            var productTask = SearchProductsGlobalAsync(query, ct);

            await Task.WhenAll(categoryTask, categoryGroupTask, productTask);

            return new GlobalSearchResultDto
            {
                Categories = categoryTask.Result,
                CategoryGroups = categoryGroupTask.Result,
                Products = productTask.Result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during global search with query: {Query}", query);
            throw;
        }
    }

    public async Task IndexProductAsync(ProductSearchDocument document, CancellationToken ct)
    {
        try
        {
            var response = await _client.IndexAsync(document, idx => idx.Index("products_v1"), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to index product {ProductId}: {Error}",
                    document.Id, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", document.Id);
            throw;
        }
    }

    public async Task IndexCategoryAsync(CategorySearchDocument document, CancellationToken ct)
    {
        try
        {
            var response = await _client.IndexAsync(document, idx => idx.Index("categories_v1"), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to index category {CategoryId}: {Error}",
                    document.Id, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing category {CategoryId}", document.Id);
            throw;
        }
    }

    public async Task IndexCategoryGroupAsync(CategoryGroupSearchDocument document, CancellationToken ct)
    {
        try
        {
            var response = await _client.IndexAsync(document, idx => idx.Index("categorygroups_v1"), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to index category group {CategoryGroupId}: {Error}",
                    document.Id, response.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing category group {CategoryGroupId}", document.Id);
            throw;
        }
    }

    #region Private Helper Methods

    private Query BuildProductQuery(SearchProductsQuery query)
    {
        var mustClauses = new List<Query>();
        var filterClauses = new List<Query>();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var searchQuery = query.Q.Trim();

            mustClauses.Add(new BoolQuery
            {
                Should = new Query[]
                {
                    new MatchPhraseQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 25
                    },
                    new MatchQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 20,
                        Fuzziness = new Fuzziness("AUTO")
                    },
                    new MatchQuery
                    {
                        Field = "name.autocomplete",
                        Query = searchQuery,
                        Boost = 10
                    },
                    new MatchQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 8
                    },
                    new MatchPhraseQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 12
                    },
                    new MatchQuery
                    {
                        Field = "categoryName.autocomplete",
                        Query = searchQuery,
                        Boost = 4
                    },
                    new MatchQuery
                    {
                        Field = "categoryName.ngram",
                        Query = searchQuery,
                        Boost = 3
                    },
                    new MatchQuery
                    {
                        Field = "categoryGroupName.autocomplete",
                        Query = searchQuery,
                        Boost = 2
                    },
                    new MatchQuery
                    {
                        Field = "categoryGroupName.ngram",
                        Query = searchQuery,
                        Boost = 1
                    },
                    new MatchQuery
                    {
                        Field = "description",
                        Query = searchQuery,
                        Boost = 1
                    }
                },
                MinimumShouldMatch = 1
            });
        }

        if (query.CategoryId.HasValue)
        {
            filterClauses.Add(new TermQuery
            {
                Field = "categoryId",
                Value = FieldValue.Long(query.CategoryId.Value)
            });
        }

        if (query.CategoryGroupId.HasValue)
        {
            filterClauses.Add(new TermQuery
            {
                Field = "categoryGroupId",
                Value = FieldValue.Long(query.CategoryGroupId.Value)
            });
        }

        if (query.IsInStock == true)
        {
            filterClauses.Add(new TermQuery
            {
                Field = "isInStock",
                Value = FieldValue.Boolean(true)
            });
        }

        if (query.HasDiscount == true)
        {
            filterClauses.Add(new TermQuery
            {
                Field = "hasDiscount",
                Value = FieldValue.Boolean(true)
            });
        }

        if (query.MinPrice.HasValue || query.MaxPrice.HasValue)
        {
            filterClauses.Add(new NumberRangeQuery
            {
                Field = "minPrice",
                Gte = query.MinPrice.HasValue ? (double)query.MinPrice.Value : null,
                Lte = query.MaxPrice.HasValue ? (double)query.MaxPrice.Value : null
            });
        }

        if (mustClauses.Count == 0 && filterClauses.Count == 0)
        {
            return new MatchAllQuery();
        }

        return new BoolQuery
        {
            Must = mustClauses.Count > 0 ? mustClauses.ToArray() : null,
            Filter = filterClauses.Count > 0 ? filterClauses.ToArray() : null
        };
    }

    private SortOptions[] BuildSortOptions(string? sort) =>
        sort?.ToLowerInvariant() switch
        {
            "price_asc" => new[]
            {
                new SortOptions
                {
                    Field = new FieldSort
                    {
                        Field = "minPrice",
                        Order = SortOrder.Asc
                    }
                }
            },
            "price_desc" => new[]
            {
                new SortOptions
                {
                    Field = new FieldSort
                    {
                        Field = "minPrice",
                        Order = SortOrder.Desc
                    }
                }
            },
            "created_desc" => new[]
            {
                new SortOptions
                {
                    Field = new FieldSort
                    {
                        Field = "createdAt",
                        Order = SortOrder.Desc
                    }
                }
            },
            "created_asc" => new[]
            {
                new SortOptions
                {
                    Field = new FieldSort
                    {
                        Field = "createdAt",
                        Order = SortOrder.Asc
                    }
                }
            },
            _ => new[]
            {
                new SortOptions
                {
                    Score = new ScoreSort
                    {
                        Order = SortOrder.Desc
                    }
                },
                new SortOptions
                {
                    Field = new FieldSort
                    {
                        Field = "createdAt",
                        Order = SortOrder.Desc
                    }
                }
            }
        };

    private async Task<IEnumerable<CategorySearchDocument>> SearchCategoriesAsync(string query, CancellationToken ct)
    {
        var searchQuery = query.Trim();

        var searchRequest = new SearchRequest("categories_v1")
        {
            Size = 5,
            Query = new BoolQuery
            {
                Should = new Query[]
                {
                    new MatchPhraseQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 25
                    },
                    new MatchQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 20,
                        Fuzziness = new Fuzziness("AUTO")
                    },
                    new MatchQuery
                    {
                        Field = "name.autocomplete",
                        Query = searchQuery,
                        Boost = 10
                    },
                    new MatchQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 8
                    },
                    new MatchPhraseQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 12
                    }
                },
                MinimumShouldMatch = 1
            }
        };

        var response = await _client.SearchAsync<CategorySearchDocument>(searchRequest, ct);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Category search failed: {Error}", response.DebugInformation);
            return [];
        }

        return response.Documents ?? [];
    }

    private async Task<IEnumerable<CategoryGroupSearchDocument>> SearchCategoryGroupsAsync(string query, CancellationToken ct)
    {
        var searchQuery = query.Trim();

        var searchRequest = new SearchRequest("categorygroups_v1")
        {
            Size = 5,
            Query = new BoolQuery
            {
                Should = new Query[]
                {
                    new MatchPhraseQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 25
                    },
                    new MatchQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 20,
                        Fuzziness = new Fuzziness("AUTO")
                    },
                    new MatchQuery
                    {
                        Field = "name.autocomplete",
                        Query = searchQuery,
                        Boost = 10
                    },
                    new MatchQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 8
                    },
                    new MatchPhraseQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 12
                    }
                },
                MinimumShouldMatch = 1
            }
        };

        var response = await _client.SearchAsync<CategoryGroupSearchDocument>(searchRequest, ct);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Category group search failed: {Error}", response.DebugInformation);
            return [];
        }

        return response.Documents ?? [];
    }

    private async Task<IEnumerable<ProductSearchDocument>> SearchProductsGlobalAsync(string query, CancellationToken ct)
    {
       var searchQuery = query.Trim();

        var searchRequest = new SearchRequest("products_v1")
        {
            Size = 5,
            Query = new BoolQuery
            {
                Should = new Query[]
                {
                    new MatchPhraseQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 25
                    },
                    new MatchQuery
                    {
                        Field = "name",
                        Query = searchQuery,
                        Boost = 20,
                        Fuzziness = new Fuzziness("AUTO")
                    },
                    new MatchQuery
                    {
                        Field = "name.autocomplete",
                        Query = searchQuery,
                        Boost = 10
                    },
                    new MatchQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 8
                    },
                    new MatchPhraseQuery
                    {
                        Field = "name.ngram",
                        Query = searchQuery,
                        Boost = 12
                    }
                },
                MinimumShouldMatch = 1
            }
        };

        var response = await _client.SearchAsync<ProductSearchDocument>(searchRequest, ct);

        if (!response.IsValidResponse)
        {
            _logger.LogWarning("Product search failed: {Error}", response.DebugInformation);
            return [];
        }

        return response.Documents ?? [];
    }

    #endregion
}