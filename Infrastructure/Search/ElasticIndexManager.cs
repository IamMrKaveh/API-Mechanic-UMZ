namespace Infrastructure.Search;

public class ElasticIndexManager : IElasticIndexManager
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticIndexManager> _logger;
    private readonly IConfiguration _configuration;

    public ElasticIndexManager(
        ElasticsearchClient client,
        ILogger<ElasticIndexManager> logger,
        IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> CreateProductIndexAsync(CancellationToken ct = default)
    {
        try
        {
            var indexName = "products_v1";

            var existsResponse = await _client.Indices.ExistsAsync(indexName, ct);
            if (existsResponse.Exists)
            {
                _logger.LogInformation("Index {IndexName} already exists", indexName);
                return true;
            }

            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(_configuration.GetValue<int>("Elasticsearch:NumberOfShards", 1))
                    .NumberOfReplicas(_configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 0))
                    .RefreshInterval(5000)
                    .MaxResultWindow(10000)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_filter", m => m
                                .Mappings(new[]
                                {
                                    "٠ => 0", "١ => 1", "٢ => 2", "٣ => 3", "٤ => 4",
                                    "٥ => 5", "٦ => 6", "٧ => 7", "٨ => 8", "٩ => 9",
                                    "ي => ی", "ك => ک", "‌ =>  ", "ة => ه", "ۀ => ه",
                                    "ً => ", "ٌ => ", "ٍ => ", "َ => ", "ُ => ", "ِ => ", "ّ => ", "ْ => "
                                })
                            )
                        )
                        .TokenFilters(tf => tf
                            .Stop("persian_stop", st => st
                                .Stopwords(new[]
                                {
                                    "و", "در", "به", "از", "که", "این", "است", "را", "با",
                                    "برای", "آن", "یک", "شود", "شده", "خود", "های", "شد",
                                    "یا", "تا", "کند", "بر", "بود", "هم", "نیز", "روی"
                                })
                            )
                            .Stemmer("persian_stemmer", st => st
                                .Language("persian")
                            )
                            .EdgeNGram("edge_ngram_filter", en => en
                                .MinGram(2)
                                .MaxGram(15)
                            )
                        )
                        .Analyzers(an => an
                            .Custom("persian_advanced", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit", "persian_stop", "persian_stemmer" })
                            )
                            .Custom("persian_autocomplete", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit", "persian_stop", "edge_ngram_filter" })
                            )
                            .Custom("persian_autocomplete_search", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit", "persian_stop" })
                            )
                        )
                        .Normalizers(n => n
                            .Custom("persian_normalizer", cn => cn
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase" })
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties<ProductSearchDocument>(p => p
                        .IntegerNumber(n => n.ProductId)
                        .Text(t => t.Name, td => td
                            .Analyzer("persian_advanced")
                            .SearchAnalyzer("persian_advanced")
                            .Fields(f => f
                                .Keyword("keyword", k => k.Normalizer("persian_normalizer"))
                                .Text("autocomplete", at => at
                                    .Analyzer("persian_autocomplete")
                                    .SearchAnalyzer("persian_autocomplete_search")
                                )
                                .Completion("suggest", c => c.Analyzer("persian_advanced"))
                            )
                        )
                        .Text(t => t.Description, td => td
                            .Analyzer("persian_advanced")
                            .SearchAnalyzer("persian_advanced")
                        )
                        .Keyword(k => k.Slug, kd => kd.Normalizer("persian_normalizer"))
                        .Keyword(k => k.Sku)
                        .Text(t => t.CategoryName, td => td
                            .Analyzer("persian_advanced")
                            .Fields(f => f.Keyword("keyword", k => k.Normalizer("persian_normalizer")))
                        )
                        .IntegerNumber(n => n.CategoryId)
                        .Text(t => t.BrandName, td => td
                            .Analyzer("persian_advanced")
                            .Fields(f => f.Keyword("keyword", k => k.Normalizer("persian_normalizer")))
                        )
                        .IntegerNumber(n => n.BrandId)
                        .FloatNumber(n => n.Price, nd => nd.GetType())
                        .FloatNumber(n => n.DiscountedPrice, nd => nd.GetType())
                        .FloatNumber(n => n.DiscountPercentage, nd => nd.GetType())
                        .Keyword(k => k.Images)
                        .Keyword(k => k.ImageUrl)
                        .Boolean(b => b.IsActive)
                        .Boolean(b => b.InStock)
                        .IntegerNumber(n => n.StockQuantity)
                        .Date(d => d.CreatedAt)
                        .Date(d => d.UpdatedAt)
                        .FloatNumber(n => n.AverageRating, nd => nd.GetType())
                        .IntegerNumber(n => n.ReviewCount)
                        .IntegerNumber(n => n.SalesCount)
                        .Keyword(k => k.Tags)
                    )
                ), ct);

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully created index {IndexName}", indexName);
                return true;
            }

            _logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating product index");
            return false;
        }
    }

    public async Task<bool> CreateCategoryIndexAsync(CancellationToken ct = default)
    {
        try
        {
            var indexName = "categories_v1";

            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_filter", m => m
                                .Mappings(new[]
                                {
                                    "ي => ی", "ك => ک", "‌ =>  ", "ة => ه", "ۀ => ه"
                                })
                            )
                        )
                        .Analyzers(an => an
                            .Custom("persian_advanced", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit" })
                            )
                        )
                        .Normalizers(n => n
                            .Custom("persian_normalizer", cn => cn
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase" })
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties<CategorySearchDocument>(p => p
                        .IntegerNumber(n => n.CategoryId)
                        .Text(t => t.Name, td => td
                            .Analyzer("persian_advanced")
                            .Fields(f => f.Keyword("keyword", k => k.Normalizer("persian_normalizer")))
                        )
                        .Keyword(k => k.Slug, kd => kd.Normalizer("persian_normalizer"))
                        .Boolean(b => b.IsActive)
                        .IntegerNumber(n => n.ProductCount)
                    )
                ), ct);

            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating category index");
            return false;
        }
    }

    public async Task<bool> CreateBrandIndexAsync(CancellationToken ct = default)
    {
        try
        {
            var indexName = "Brands_v1";

            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_filter", m => m
                                .Mappings(new[] { "ي => ی", "ك => ک" })
                            )
                        )
                        .Analyzers(an => an
                            .Custom("persian_advanced", ca => ca
                                .Tokenizer("standard")
                                .Filter(new[] { "lowercase" })
                            )
                        )
                        .Normalizers(n => n
                            .Custom("persian_normalizer", cn => cn
                                .Filter(new[] { "lowercase" })
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties<BrandSearchDocument>(p => p
                        .IntegerNumber(n => n.BrandId)
                        .Text(t => t.Name, td => td
                            .Analyzer("persian_advanced")
                            .Fields(f => f.Keyword("keyword", k => k.Normalizer("persian_normalizer")))
                        )
                        .Keyword(k => k.Slug, kd => kd.Normalizer("persian_normalizer"))
                        .IntegerNumber(n => n.CategoryId)
                        .Text(t => t.CategoryName, td => td.Analyzer("persian_advanced"))
                        .Boolean(b => b.IsActive)
                        .IntegerNumber(n => n.ProductCount)
                    )
                ), ct);

            return response.IsValidResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating category group index");
            return false;
        }
    }

    public async Task<bool> CreateAllIndicesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Creating all Elasticsearch indices...");

        var productResult = await CreateProductIndexAsync(ct);
        var categoryResult = await CreateCategoryIndexAsync(ct);
        var BrandResult = await CreateBrandIndexAsync(ct);

        var success = productResult && categoryResult && BrandResult;

        if (success)
        {
            _logger.LogInformation("All indices created successfully");
        }
        else
        {
            _logger.LogWarning("Some indices failed to create");
        }

        return success;
    }

    public async Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default)
    {
        var response = await _client.Indices.ExistsAsync(indexName, ct);
        return response.Exists;
    }

    public async Task<bool> DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.Indices.DeleteAsync(indexName, ct);

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully deleted index {IndexName}", indexName);
                return true;
            }

            _logger.LogError("Failed to delete index {IndexName}", indexName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while deleting index {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> ReindexAsync(string sourceIndex, string destinationIndex, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.ReindexAsync(r => r
                .Source(s => s.Indices(sourceIndex))
                .Dest(d => d.Index(destinationIndex)), ct);

            if (response.IsValidResponse)
            {
                _logger.LogInformation("Successfully reindexed from {Source} to {Destination}", sourceIndex, destinationIndex);
                return true;
            }

            _logger.LogError("Failed to reindex from {Source} to {Destination}", sourceIndex, destinationIndex);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while reindexing");
            return false;
        }
    }
}