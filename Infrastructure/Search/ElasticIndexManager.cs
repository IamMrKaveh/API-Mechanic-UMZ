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
        try
        {
            var productTask = CreateProductIndexAsync(ct);
            var categoryTask = CreateCategoryIndexAsync(ct);
            var categoryGroupTask = CreateCategoryGroupIndexAsync(ct);

            var results = await Task.WhenAll(productTask, categoryTask, categoryGroupTask);

            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create all indices");
            return false;
        }
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

            var numberOfShards = isProduction
                ? _configuration.GetValue<int>("Elasticsearch:NumberOfShards", 3)
                : 1;

            var numberOfReplicas = isProduction
                ? _configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 1)
                : 0;

            var refreshInterval = isProduction ? "30s" : "1s";
            var maxResultWindow = _configuration.GetValue<int>("Elasticsearch:MaxResultWindow", 10000);

            var createResponse = await _client.Indices.CreateAsync<ProductSearchDocument>(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                    .MaxResultWindow(maxResultWindow)
                    .RefreshInterval(refreshInterval)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("autocomplete", ca => ca
                                .Tokenizer("standard")
                                .Filter(["lowercase", "autocomplete_filter"])
                            )
                        )
                        .TokenFilters(tf => tf
                            .EdgeNGram("autocomplete_filter", eg => eg
                                .MinGram(2)
                                .MaxGram(20)
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties(p => p
                        .IntegerNumber(prop => prop.Id)
                        .Text(prop => prop.Name, td => td
                            .Fields(f => f
                                .Text("autocomplete", txt => txt
                                    .Analyzer("autocomplete")
                                    .SearchAnalyzer("standard")
                                )
                                .Completion("completion")
                            )
                        )
                        .Text(prop => prop.Description)
                        .Keyword(prop => prop.CategoryName)
                        .IntegerNumber(prop => prop.CategoryId)
                        .Keyword(prop => prop.CategoryGroupName)
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

            var numberOfShards = isProduction
                ? _configuration.GetValue<int>("Elasticsearch:NumberOfShards", 3)
                : 1;

            var numberOfReplicas = isProduction
                ? _configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 1)
                : 0;

            var createResponse = await _client.Indices.CreateAsync<CategorySearchDocument>(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("autocomplete", ca => ca
                                .Tokenizer("standard")
                                .Filter(["lowercase", "autocomplete_filter"])
                            )
                        )
                        .TokenFilters(tf => tf
                            .EdgeNGram("autocomplete_filter", eg => eg
                                .MinGram(2)
                                .MaxGram(20)
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties(p => p
                        .IntegerNumber(prop => prop.Id)
                        .Text(prop => prop.Name, td => td
                            .Fields(f => f
                                .Text("autocomplete", txt => txt
                                    .Analyzer("autocomplete")
                                    .SearchAnalyzer("standard")
                                )
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

            _logger.LogInformation("Successfully created index {IndexName}", indexName);
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

            var numberOfShards = isProduction
                ? _configuration.GetValue<int>("Elasticsearch:NumberOfShards", 3)
                : 1;

            var numberOfReplicas = isProduction
                ? _configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 1)
                : 0;

            var createResponse = await _client.Indices.CreateAsync<CategoryGroupSearchDocument>(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(numberOfShards)
                    .NumberOfReplicas(numberOfReplicas)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("autocomplete", ca => ca
                                .Tokenizer("standard")
                                .Filter(["lowercase", "autocomplete_filter"])
                            )
                        )
                        .TokenFilters(tf => tf
                            .EdgeNGram("autocomplete_filter", eg => eg
                                .MinGram(2)
                                .MaxGram(20)
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties(p => p
                        .IntegerNumber(prop => prop.Id)
                        .Text(prop => prop.Name, td => td
                            .Fields(f => f
                                .Text("autocomplete", txt => txt
                                    .Analyzer("autocomplete")
                                    .SearchAnalyzer("standard")
                                )
                            )
                        )
                        .Keyword(prop => prop.CategoryName)
                        .IntegerNumber(prop => prop.CategoryId)
                        .Keyword(prop => prop.IconUrl)
                    )
                ), ct);

            if (!createResponse.IsValidResponse)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}",
                    indexName, createResponse.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully created index {IndexName}", indexName);
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
            var deleteResponse = await _client.Indices.DeleteAsync(indexName, cancellationToken: ct);

            if (!deleteResponse.IsValidResponse)
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}",
                    indexName, deleteResponse.DebugInformation);
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
            var existsResponse = await _client.Indices.ExistsAsync(indexName, cancellationToken: ct);
            return existsResponse.Exists;
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
            var reindexResponse = await _client.ReindexAsync(r => r
                .Source(s => s.Indices(sourceIndex))
                .Dest(d => d.Index(destinationIndex)), ct);

            if (!reindexResponse.IsValidResponse)
            {
                _logger.LogError("Failed to reindex from {SourceIndex} to {DestinationIndex}: {Error}",
                    sourceIndex, destinationIndex, reindexResponse.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully reindexed from {SourceIndex} to {DestinationIndex}",
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