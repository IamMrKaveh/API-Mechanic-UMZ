namespace Infrastructure.Search;

public class ElasticBulkService : IElasticBulkService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticBulkService> _logger;
    private readonly IConfiguration _configuration;

    public ElasticBulkService(
        ElasticsearchClient client,
        ILogger<ElasticBulkService> logger,
        IConfiguration configuration)
    {
        _client = client;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> BulkIndexProductsAsync(
    IEnumerable<ProductSearchDocument> products,
    CancellationToken ct = default)
    {
        try
        {
            var productList = products.ToList();
            if (!productList.Any())
            {
                _logger.LogWarning("No products to index");
                return true;
            }

            _logger.LogInformation("Starting bulk indexing of {Count} products", productList.Count);

            var batchSize = _configuration.GetValue<int>("Elasticsearch:BulkBatchSize", 1000);
            var bulkTimeout = _configuration.GetValue<int>("Elasticsearch:BulkTimeoutSeconds", 120);
            var batches = productList.Chunk(batchSize);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(bulkTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var successCount = 0;
            var failureCount = 0;

            foreach (var batch in batches)
            {
                try
                {
                    var bulkResponse = await _client.BulkAsync(b => b
                        .Index("products")
                        .IndexMany(batch)
                        .Refresh(Elastic.Clients.Elasticsearch.Refresh.False), linkedCts.Token);

                    if (!bulkResponse.IsValidResponse || bulkResponse.Errors)
                    {
                        failureCount += batch.Length;
                        _logger.LogError("Bulk indexing failed for batch. Errors: {Errors}",
                            string.Join(", ", bulkResponse.ItemsWithErrors.Select(i => i.Error?.Reason ?? "Unknown")));
                    }
                    else
                    {
                        successCount += batch.Length;
                    }
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    _logger.LogError(
                        "Bulk indexing timeout after {Timeout} seconds. Processed {Success}/{Total}",
                        bulkTimeout, successCount, productList.Count);
                    failureCount += batch.Length;
                    break;
                }
            }

            _logger.LogInformation(
                "Bulk indexing completed. Success: {Success}, Failures: {Failures}",
                successCount, failureCount);

            return failureCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk indexing of products");
            return false;
        }
    }

    public async Task<bool> BulkIndexCategoriesAsync(IEnumerable<CategorySearchDocument> categories, CancellationToken ct = default)
    {
        try
        {
            var categoryList = categories.ToList();
            if (!categoryList.Any())
            {
                _logger.LogWarning("No categories to index");
                return true;
            }

            _logger.LogInformation("Starting bulk indexing of {Count} categories", categoryList.Count);

            var bulkResponse = await _client.BulkAsync(b => b
                .Index("categories_v1")
                .IndexMany(categoryList)
                .Refresh(Elastic.Clients.Elasticsearch.Refresh.False), ct);

            if (!bulkResponse.IsValidResponse || bulkResponse.Errors)
            {
                _logger.LogError("Bulk indexing of categories failed. Errors: {Errors}",
                    string.Join(", ", bulkResponse.ItemsWithErrors.Select(i => i.Error?.Reason ?? "Unknown")));
                return false;
            }

            _logger.LogInformation("Successfully indexed {Count} categories", categoryList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk indexing of categories");
            return false;
        }
    }

    public async Task<bool> BulkIndexCategoryGroupsAsync(IEnumerable<CategoryGroupSearchDocument> categoryGroups, CancellationToken ct = default)
    {
        try
        {
            var groupList = categoryGroups.ToList();
            if (!groupList.Any())
            {
                _logger.LogWarning("No category groups to index");
                return true;
            }

            _logger.LogInformation("Starting bulk indexing of {Count} category groups", groupList.Count);

            var bulkResponse = await _client.BulkAsync(b => b
                .Index("categorygroups_v1")
                .IndexMany(groupList)
                .Refresh(Elastic.Clients.Elasticsearch.Refresh.False), ct);

            if (!bulkResponse.IsValidResponse || bulkResponse.Errors)
            {
                _logger.LogError("Bulk indexing of category groups failed. Errors: {Errors}",
                    string.Join(", ", bulkResponse.ItemsWithErrors.Select(i => i.Error?.Reason ?? "Unknown")));
                return false;
            }

            _logger.LogInformation("Successfully indexed {Count} category groups", groupList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk indexing of category groups");
            return false;
        }
    }

    public async Task<bool> BulkDeleteProductsAsync(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        try
        {
            var idList = productIds.ToList();
            if (!idList.Any())
            {
                _logger.LogWarning("No products to delete");
                return true;
            }

            _logger.LogInformation("Starting bulk deletion of {Count} products", idList.Count);

            var bulkResponse = await _client.BulkAsync(b =>
            {
                b.Index("products_v1");
                foreach (var id in idList)
                {
                    b.Delete<ProductSearchDocument>(d => d.Id(id));
                }
                b.Refresh(Elastic.Clients.Elasticsearch.Refresh.False);
            }, ct);

            if (!bulkResponse.IsValidResponse || bulkResponse.Errors)
            {
                _logger.LogError("Bulk deletion failed. Errors: {Errors}",
                    string.Join(", ", bulkResponse.ItemsWithErrors.Select(i => i.Error?.Reason ?? "Unknown")));
                return false;
            }

            _logger.LogInformation("Successfully deleted {Count} products", idList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk deletion of products");
            return false;
        }
    }

    public async Task<bool> BulkUpdateProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
    {
        try
        {
            var productList = products.ToList();
            if (!productList.Any())
            {
                _logger.LogWarning("No products to update");
                return true;
            }

            _logger.LogInformation("Starting bulk update of {Count} products", productList.Count);

            var batchSize = _configuration.GetValue<int>("Elasticsearch:BulkBatchSize", 1000);
            var batches = productList.Chunk(batchSize);

            var successCount = 0;
            var failureCount = 0;

            foreach (var batch in batches)
            {
                var bulkResponse = await _client.BulkAsync(b =>
                {
                    b.Index("products_v1");
                    foreach (var product in batch)
                    {
                        b.Update<ProductSearchDocument, ProductSearchDocument>(u => u
                            .Id(product.Id)
                            .Doc(product)
                            .DocAsUpsert(true));
                    }
                    b.Refresh(Elastic.Clients.Elasticsearch.Refresh.False);
                }, ct);

                if (!bulkResponse.IsValidResponse || bulkResponse.Errors)
                {
                    failureCount += batch.Length;
                    _logger.LogError("Bulk update failed for batch. Errors: {Errors}",
                        string.Join(", ", bulkResponse.ItemsWithErrors.Select(i => i.Error?.Reason ?? "Unknown")));
                }
                else
                {
                    successCount += batch.Length;
                }
            }

            _logger.LogInformation("Bulk update completed. Success: {Success}, Failures: {Failures}",
                successCount, failureCount);

            return failureCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk update of products");
            return false;
        }
    }
}