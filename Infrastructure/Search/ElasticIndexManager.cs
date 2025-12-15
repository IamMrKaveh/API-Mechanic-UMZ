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

    public async Task<bool> CreateAllIndicesAsync(CancellationToken ct = default)
    {
        var productTask = CreateProductIndexAsync(ct);
        var categoryTask = CreateCategoryIndexAsync(ct);
        var categoryGroupTask = CreateCategoryGroupIndexAsync(ct);

        var results = await Task.WhenAll(productTask, categoryTask, categoryGroupTask);

        var allSuccess = results.All(r => r);

        if (allSuccess)
        {
            _logger.LogInformation("Successfully created all indices");
        }
        else
        {
            _logger.LogWarning("Some indices failed to create");
        }

        return allSuccess;
    }

    public async Task<bool> CreateProductIndexAsync(CancellationToken ct = default)
    {
        const string indexName = "products_v1";
        try
        {
            if (await IndexExistsAsync(indexName, ct))
            {
                _logger.LogInformation("Index {IndexName} already exists", indexName);
                return true;
            }
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var isProduction = environment == "Production";
            var numberOfShards = isProduction ? _configuration.GetValue<int>("Elasticsearch:NumberOfShards", 3) : 1;
            var numberOfReplicas = isProduction ? _configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 1) : 0;
            var refreshInterval = isProduction ? "30s" : "1s";
            var maxResultWindow = _configuration.GetValue<int>("Elasticsearch:MaxResultWindow", 10000);

            var createResponse = await _client.Indices.CreateAsync<ProductSearchDocument>(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                    .RefreshInterval(refreshInterval)
                    .MaxResultWindow(maxResultWindow)
                    .MaxNgramDiff(20)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_mapping", m => m
                                .Mappings([
                                    "ك => ک",
                                "ي => ی",
                                "ؤ => و",
                                "إ => ا",
                                "أ => ا",
                                "ـ => ",
                                "ۀ => ه",
                                "ة => ه"
                                ])
                            )
                        )
                        .Analyzers(an => an
                            .Custom("persian_text", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "persian_stop", "persian_stemmer"])
                            )
                            .Custom("persian_search", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit"])
                            )
                            .Custom("persian_autocomplete", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "autocomplete_filter"])
                            )
                            .Custom("persian_ngram", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "ngram_filter"])
                            )
                        )
                        .TokenFilters(tf => tf
                            .Stop("persian_stop", st => st
                                .Stopwords(StopWordLanguage.Persian)
                            )
                            .Stemmer("persian_stemmer", sm => sm
                                .Language("persian")
                            )
                            .EdgeNGram("autocomplete_filter", eg => eg.MinGram(2).MaxGram(20))
                            .NGram("ngram_filter", ng => ng.MinGram(2).MaxGram(10))
                        )
                    )
                )
                .Mappings(m => m
                    .Properties(p => p
                        .IntegerNumber(prop => prop.Id)
                        .Text(prop => prop.Name, td => td
                            .Analyzer("persian_text")
                            .SearchAnalyzer("persian_search")
                            .Fields(f => f
                                .Text("autocomplete", txt => txt
                                    .Analyzer("persian_autocomplete")
                                    .SearchAnalyzer("persian_search")
                                )
                                .Text("ngram", txt => txt
                                    .Analyzer("persian_ngram")
                                    .SearchAnalyzer("persian_search")
                                )
                                .Keyword("keyword")
                            )
                        )
                        .Text(prop => prop.Description, td => td
                            .Analyzer("persian_text")
                            .SearchAnalyzer("persian_search")
                        )
                        .Text(prop => prop.CategoryName, td => td
                            .Analyzer("persian_text")
                            .SearchAnalyzer("persian_search")
                            .Fields(f => f
                                .Keyword("keyword")
                            )
                        )
                        .IntegerNumber(prop => prop.CategoryId)
                        .Text(prop => prop.CategoryGroupName, td => td
                            .Analyzer("persian_text")
                            .SearchAnalyzer("persian_search")
                            .Fields(f => f
                                .Keyword("keyword")
                            )
                        )
                        .IntegerNumber(prop => prop.CategoryGroupId)
                        .ScaledFloatNumber(prop => prop.MinPrice, nd => nd.ScalingFactor(100))
                        .ScaledFloatNumber(prop => prop.MaxPrice, nd => nd.ScalingFactor(100))
                        .Boolean(prop => prop.HasDiscount)
                        .Boolean(prop => prop.IsInStock)
                        .Date(prop => prop.CreatedAt)
                        .Keyword(prop => prop.ImageUrl)
                    )
                ), ct);

            if (!createResponse.IsValidResponse)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}",
                    indexName, createResponse.DebugInformation);
                return false;
            }
            _logger.LogInformation("Successfully created index {IndexName} with {Shards} shards and {Replicas} replicas",
                indexName, numberOfShards, numberOfReplicas);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> CreateCategoryIndexAsync(CancellationToken ct = default)
    {
        const string indexName = "categories_v1";
        try
        {
            if (await IndexExistsAsync(indexName, ct))
            {
                _logger.LogInformation("Index {IndexName} already exists", indexName);
                return true;
            }
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var isProduction = environment == "Production";
            var numberOfShards = isProduction ? _configuration.GetValue<int>("Elasticsearch:NumberOfShards", 3) : 1;
            var numberOfReplicas = isProduction ? _configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 1) : 0;
            var refreshInterval = isProduction ? "30s" : "1s";
            var maxResultWindow = _configuration.GetValue<int>("Elasticsearch:MaxResultWindow", 10000);

            var createResponse = await _client.Indices.CreateAsync<CategorySearchDocument>(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                    .RefreshInterval(refreshInterval)
                    .MaxResultWindow(maxResultWindow)
                    .MaxNgramDiff(20)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_mapping", m => m
                                .Mappings([
                                    "ك => ک",
                                "ي => ی",
                                "ؤ => و",
                                "إ => ا",
                                "أ => ا",
                                "ـ => ",
                                "ۀ => ه",
                                "ة => ه"
                                ])
                            )
                        )
                        .Analyzers(an => an
                            .Custom("persian_text", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "persian_stop", "persian_stemmer"])
                            )
                            .Custom("persian_search", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit"])
                            )
                            .Custom("persian_autocomplete", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "autocomplete_filter"])
                            )
                            .Custom("persian_ngram", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "ngram_filter"])
                            )
                        )
                        .TokenFilters(tf => tf
                            .Stop("persian_stop", st => st
                                .Stopwords(StopWordLanguage.Persian)
                            )
                            .Stemmer("persian_stemmer", sm => sm
                                .Language("persian")
                            )
                            .EdgeNGram("autocomplete_filter", eg => eg.MinGram(2).MaxGram(20))
                            .NGram("ngram_filter", ng => ng.MinGram(2).MaxGram(10))
                        )
                    )
                )
                .Mappings(m => m
                    .Properties(p => p
                        .IntegerNumber(prop => prop.Id)
                        .Text(prop => prop.Name, td => td
                            .Analyzer("persian_text")
                            .SearchAnalyzer("persian_search")
                            .Fields(f => f
                                .Text("autocomplete", txt => txt
                                    .Analyzer("persian_autocomplete")
                                    .SearchAnalyzer("persian_search")
                                )
                                .Text("ngram", txt => txt
                                    .Analyzer("persian_ngram")
                                    .SearchAnalyzer("persian_search")
                                )
                                .Keyword("keyword")
                            )
                        )
                        .Keyword(prop => prop.IconUrl)
                    )
                ), ct);

            if (!createResponse.IsValidResponse)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}",
                    indexName, createResponse.DebugInformation);
                return false;
            }
            _logger.LogInformation("Successfully created index {IndexName} with {Shards} shards and {Replicas} replicas",
                indexName, numberOfShards, numberOfReplicas);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> CreateCategoryGroupIndexAsync(CancellationToken ct = default)
    {
        const string indexName = "categorygroups_v1";
        try
        {
            if (await IndexExistsAsync(indexName, ct))
            {
                _logger.LogInformation("Index {IndexName} already exists", indexName);
                return true;
            }
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var isProduction = environment == "Production";
            var numberOfShards = isProduction ? _configuration.GetValue<int>("Elasticsearch:NumberOfShards", 3) : 1;
            var numberOfReplicas = isProduction ? _configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 1) : 0;
            var refreshInterval = isProduction ? "30s" : "1s";
            var maxResultWindow = _configuration.GetValue<int>("Elasticsearch:MaxResultWindow", 10000);

            var createResponse = await _client.Indices.CreateAsync<CategoryGroupSearchDocument>(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                    .RefreshInterval(refreshInterval)
                    .MaxResultWindow(maxResultWindow)
                    .MaxNgramDiff(20)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_mapping", m => m
                                .Mappings([
                                    "ك => ک",
                                "ي => ی",
                                "ؤ => و",
                                "إ => ا",
                                "أ => ا",
                                "ـ => ",
                                "ۀ => ه",
                                "ة => ه"
                                ])
                            )
                        )
                        .Analyzers(an => an
                            .Custom("persian_text", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "persian_stop", "persian_stemmer"])
                            )
                            .Custom("persian_search", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit"])
                            )
                            .Custom("persian_autocomplete", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "autocomplete_filter"])
                            )
                            .Custom("persian_ngram", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(["persian_char_mapping"])
                                .Filter(["lowercase", "arabic_normalization", "persian_normalization", "decimal_digit", "ngram_filter"])
                            )
                        )
                        .TokenFilters(tf => tf
                            .Stop("persian_stop", st => st
                                .Stopwords(StopWordLanguage.Persian)
                            )
                            .Stemmer("persian_stemmer", sm => sm
                                .Language("persian")
                            )
                            .EdgeNGram("autocomplete_filter", eg => eg.MinGram(2).MaxGram(20))
                            .NGram("ngram_filter", ng => ng.MinGram(2).MaxGram(10))
                        )
                    )
                )
                .Mappings(m => m
                    .Properties(p => p
                        .IntegerNumber(prop => prop.Id)
                        .Text(prop => prop.Name, td => td
                            .Analyzer("persian_text")
                            .SearchAnalyzer("persian_search")
                            .Fields(f => f
                                .Text("autocomplete", txt => txt
                                    .Analyzer("persian_autocomplete")
                                    .SearchAnalyzer("persian_search")
                                )
                                .Text("ngram", txt => txt
                                    .Analyzer("persian_ngram")
                                    .SearchAnalyzer("persian_search")
                                )
                                .Keyword("keyword")
                            )
                        )
                        .Keyword(prop => prop.IconUrl)
                    )
                ), ct);

            if (!createResponse.IsValidResponse)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}",
                    indexName, createResponse.DebugInformation);
                return false;
            }
            _logger.LogInformation("Successfully created index {IndexName} with {Shards} shards and {Replicas} replicas",
                indexName, numberOfShards, numberOfReplicas);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            if (!await IndexExistsAsync(indexName, ct))
            {
                _logger.LogInformation("Index {IndexName} does not exist", indexName);
                return true;
            }

            var response = await _client.Indices.DeleteAsync(indexName, ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}",
                    indexName, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully deleted index {IndexName}", indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting index {IndexName}", indexName);
            return false;
        }
    }

    public async Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.Indices.ExistsAsync(indexName, ct);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if index {IndexName} exists", indexName);
            return false;
        }
    }

    public async Task<bool> ReindexAsync(string sourceIndex, string destinationIndex, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.ReindexAsync(r => r
                .Source(s => s.Indices(sourceIndex))
                .Dest(d => d.Index(destinationIndex))
                .WaitForCompletion(false), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to reindex from {SourceIndex} to {DestinationIndex}: {Error}",
                    sourceIndex, destinationIndex, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully started reindex from {SourceIndex} to {DestinationIndex}",
                sourceIndex, destinationIndex);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reindexing from {SourceIndex} to {DestinationIndex}",
                sourceIndex, destinationIndex);
            return false;
        }
    }
}