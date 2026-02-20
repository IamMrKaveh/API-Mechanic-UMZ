namespace Infrastructure.Search.Services;

public class ElasticBulkService : IElasticBulkService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticBulkService> _logger;
    private readonly ElasticsearchMetrics _metrics;

    public ElasticBulkService(
        ElasticsearchClient client,
        ILogger<ElasticBulkService> logger,
        ElasticsearchMetrics metrics)
    {
        _client = client;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool> BulkIndexProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
    {
        try
        {
            var productList = products.ToList();
            if (!productList.Any())
            {
                _logger.LogWarning("No products to index");
                return true;
            }

            var response = await _client.BulkAsync(b => b
                .Index("products_v1")
                .IndexMany(productList, (op, doc) => op
                    .Index("products_v1")
                    .Id(doc.ProductId)
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Bulk index products failed: {Error}", response.DebugInformation);
                _metrics.RecordBulkOperationFailure("products_v1");
                return false;
            }

            if (response.Errors)
            {
                var failedItems = response.Items
                    .Where(i => i.Error != null)
                    .Select(i => new { i.Id, i?.Error?.Reason })
                    .ToList();

                _logger.LogWarning("Bulk index completed with {Count} errors: {Errors}",
                    failedItems.Count,
                    JsonSerializer.Serialize(failedItems));

                _metrics.RecordBulkOperationPartialFailure(failedItems.Count, "products_v1");
            }
            else
            {
                _metrics.RecordBulkOperationSuccess(productList.Count, "products_v1");
            }

            _logger.LogInformation("Successfully bulk indexed {Count} products", productList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bulk index products");
            _metrics.RecordBulkOperationFailure("products_v1");
            throw;
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

            var response = await _client.BulkAsync(b => b
                .Index("categories_v1")
                .IndexMany(categoryList, (op, doc) => op
                    .Index("categories_v1")
                    .Id(doc.CategoryId)
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Bulk index categories failed: {Error}", response.DebugInformation);
                _metrics.RecordBulkOperationFailure("categories_v1");
                return false;
            }

            if (response.Errors)
            {
                var failedItems = response.Items
                    .Where(i => i.Error != null)
                    .Select(i => new { i.Id, i?.Error?.Reason })
                    .ToList();

                _logger.LogWarning("Bulk index categories completed with {Count} errors: {Errors}",
                    failedItems.Count,
                    JsonSerializer.Serialize(failedItems));

                _metrics.RecordBulkOperationPartialFailure(failedItems.Count, "categories_v1");
            }
            else
            {
                _metrics.RecordBulkOperationSuccess(categoryList.Count, "categories_v1");
            }

            _logger.LogInformation("Successfully bulk indexed {Count} categories", categoryList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bulk index categories");
            _metrics.RecordBulkOperationFailure("categories_v1");
            throw;
        }
    }

    public async Task<bool> BulkIndexBrandsAsync(IEnumerable<BrandSearchDocument> Brands, CancellationToken ct = default)
    {
        try
        {
            var BrandList = Brands.ToList();
            if (!BrandList.Any())
            {
                _logger.LogWarning("No category groups to index");
                return true;
            }

            var response = await _client.BulkAsync(b => b
                .Index("Brands_v1")
                .IndexMany(BrandList, (op, doc) => op
                    .Index("Brands_v1")
                    .Id(doc.BrandId)
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Bulk index category groups failed: {Error}", response.DebugInformation);
                _metrics.RecordBulkOperationFailure("Brands_v1");
                return false;
            }

            if (response.Errors)
            {
                var failedItems = response.Items
                    .Where(i => i.Error != null)
                    .Select(i => new { i.Id, i?.Error?.Reason })
                    .ToList();

                _logger.LogWarning("Bulk index category groups completed with {Count} errors: {Errors}",
                    failedItems.Count,
                    JsonSerializer.Serialize(failedItems));

                _metrics.RecordBulkOperationPartialFailure(failedItems.Count, "Brands_v1");
            }
            else
            {
                _metrics.RecordBulkOperationSuccess(BrandList.Count, "Brands_v1");
            }

            _logger.LogInformation("Successfully bulk indexed {Count} category groups", BrandList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bulk index category groups");
            _metrics.RecordBulkOperationFailure("Brands_v1");
            throw;
        }
    }

    public async Task<bool> BulkDeleteProductsAsync(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        try
        {
            var productIdList = productIds.ToList();
            if (!productIdList.Any())
            {
                _logger.LogWarning("No products to delete");
                return true;
            }

            var response = await _client.BulkAsync(b => b
                .Index("products_v1")
                .DeleteMany(productIdList, (op, id) => op
                    .Index("products_v1")
                    .Id(id)
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Bulk delete products failed: {Error}", response.DebugInformation);
                _metrics.RecordBulkOperationFailure("products_v1");
                return false;
            }

            if (response.Errors)
            {
                var failedItems = response.Items
                    .Where(i => i.Error != null)
                    .Select(i => new { i.Id, i?.Error?.Reason })
                    .ToList();

                _logger.LogWarning("Bulk delete completed with {Count} errors: {Errors}",
                    failedItems.Count,
                    JsonSerializer.Serialize(failedItems));

                _metrics.RecordBulkOperationPartialFailure(failedItems.Count, "products_v1");
            }
            else
            {
                _metrics.RecordBulkOperationSuccess(productIdList.Count, "products_v1");
            }

            _logger.LogInformation("Successfully bulk deleted {Count} products", productIdList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bulk delete products");
            _metrics.RecordBulkOperationFailure("products_v1");
            throw;
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

            var response = await _client.BulkAsync(b => b
                .Index("products_v1")
                .UpdateMany(productList, (op, doc) => op
                    .Index("products_v1")
                    .Id(doc.ProductId)
                    .Doc(doc)
                ), ct);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Bulk update products failed: {Error}", response.DebugInformation);
                _metrics.RecordBulkOperationFailure("products_v1");
                return false;
            }

            if (response.Errors)
            {
                var failedItems = response.Items
                    .Where(i => i.Error != null)
                    .Select(i => new { i.Id, i?.Error?.Reason })
                    .ToList();

                _logger.LogWarning("Bulk update completed with {Count} errors: {Errors}",
                    failedItems.Count,
                    JsonSerializer.Serialize(failedItems));

                _metrics.RecordBulkOperationPartialFailure(failedItems.Count, "products_v1");
            }
            else
            {
                _metrics.RecordBulkOperationSuccess(productList.Count, "products_v1");
            }

            _logger.LogInformation("Successfully bulk updated {Count} products", productList.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during bulk update products");
            _metrics.RecordBulkOperationFailure("products_v1");
            throw;
        }
    }
}