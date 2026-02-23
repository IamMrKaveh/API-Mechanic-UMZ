namespace Infrastructure.Search.Services;

/// <summary>
/// استفاده از Dapper به‌جای EF Core Include برای sync عملکردی
/// داده‌ها با کوئری‌های flat و paginated خوانده می‌شوند
/// هر batch در scope جداگانه پردازش می‌شود تا Change Tracker بلوت نشود
/// </summary>
public class ElasticsearchDatabaseSyncService : ISearchDatabaseSyncService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly ISearchService _searchService;
    private readonly IElasticBulkService _bulkService;
    private readonly ILogger<ElasticsearchDatabaseSyncService> _logger;

    public ElasticsearchDatabaseSyncService(
        ISqlConnectionFactory sqlConnectionFactory,
        ISearchService searchService,
        IElasticBulkService bulkService,
        ILogger<ElasticsearchDatabaseSyncService> logger)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _searchService = searchService;
        _bulkService = bulkService;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        await FullSyncAsync(ct);
        _logger.LogInformation("Sync Completed");
    }

    /// <summary>
    /// استفاده از Dapper با کوئری flat به‌جای EF Include chain
    /// </summary>
    public async Task SyncProductAsync(int productId, CancellationToken ct = default)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                p.Id             AS ProductId,
                p.Name           AS Name,
                p.Description    AS Description,
                p.Slug           AS Slug,
                b.Id             AS BrandId,
                b.Name           AS BrandName,
                b.Slug           AS BrandSlug,
                c.Id             AS CategoryId,
                c.Name           AS CategoryName,
                MIN(pv.SellingPrice)  AS Price,
                SUM(pv.StockQuantity) AS StockQuantity,
                p.IsActive       AS IsActive,
                p.CreatedAt      AS CreatedAt,
                p.UpdatedAt      AS UpdatedAt
            FROM Products p
            INNER JOIN Brands b ON p.BrandId = b.Id
            INNER JOIN Categories c ON b.CategoryId = c.Id
            LEFT JOIN ProductVariants pv ON pv.ProductId = p.Id AND pv.IsDeleted = 0
            WHERE p.Id = @ProductId AND p.IsDeleted = 0
            GROUP BY p.Id, p.Name, p.Description, p.Slug, b.Id, b.Name, b.Slug, c.Id, c.Name, p.IsActive, p.CreatedAt, p.UpdatedAt";

        var document = await connection.QueryFirstOrDefaultAsync<ProductSearchDocument>(sql, new { ProductId = productId });

        if (document == null)
        {
            _logger.LogWarning("Product {ProductId} not found for sync", productId);
            return;
        }

        await _searchService.IndexProductAsync(document, ct);
        _logger.LogInformation("Product {ProductId} synced to Elasticsearch", productId);
    }

    /// <summary>
    /// خواندن paginated با Dapper - هر batch یک connection جدید
    /// </summary>
    public async Task SyncAllProductsAsync(CancellationToken ct = default)
    {
        const int batchSize = 500;
        var offset = 0;
        var hasMore = true;

        const string sql = @"
            SELECT
                p.Id             AS ProductId,
                p.Name           AS Name,
                p.Description    AS Description,
                p.Slug           AS Slug,
                b.Id             AS BrandId,
                b.Name           AS BrandName,
                b.Slug           AS BrandSlug,
                c.Id             AS CategoryId,
                c.Name           AS CategoryName,
                MIN(pv.SellingPrice)  AS Price,
                COALESCE(SUM(pv.StockQuantity), 0) AS StockQuantity,
                CAST(CASE WHEN SUM(CASE WHEN pv.IsUnlimited = 1 THEN 1 ELSE 0 END) > 0
                     OR COALESCE(SUM(pv.StockQuantity), 0) > 0 THEN 1 ELSE 0 END AS BIT) AS InStock,
                p.IsActive       AS IsActive,
                p.CreatedAt      AS CreatedAt,
                p.UpdatedAt      AS UpdatedAt
            FROM Products p
            INNER JOIN Brands b ON p.BrandId = b.Id
            INNER JOIN Categories c ON b.CategoryId = c.Id
            LEFT JOIN ProductVariants pv ON pv.ProductId = p.Id AND pv.IsDeleted = 0 AND pv.IsActive = 1
            WHERE p.IsActive = 1 AND p.IsDeleted = 0
            GROUP BY p.Id, p.Name, p.Description, p.Slug, b.Id, b.Name, b.Slug, c.Id, c.Name, p.IsActive, p.CreatedAt, p.UpdatedAt
            ORDER BY p.Id
            OFFSET @Offset ROWS FETCH NEXT @BatchSize ROWS ONLY";

        while (hasMore && !ct.IsCancellationRequested)
        {
            using var connection = _sqlConnectionFactory.CreateConnection();

            var documents = (await connection.QueryAsync<ProductSearchDocument>(
                sql, new { Offset = offset, BatchSize = batchSize })).ToList();

            if (documents.Count == 0)
            {
                hasMore = false;
                continue;
            }

            await _bulkService.BulkIndexProductsAsync(documents, ct);
            _logger.LogInformation("Synced batch at offset {Offset} with {Count} products", offset, documents.Count);

            offset += batchSize;

            if (documents.Count < batchSize) hasMore = false;

            await Task.Delay(100, ct);
        }

        _logger.LogInformation("Completed syncing all products");
    }

    public async Task SyncCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                c.Id        AS CategoryId,
                c.Name      AS Name,
                c.IsActive  AS IsActive,
                COUNT(DISTINCT p.Id) AS ProductCount
            FROM Categories c
            LEFT JOIN Brands b ON b.CategoryId = c.Id AND b.IsDeleted = 0
            LEFT JOIN Products p ON p.BrandId = b.Id AND p.IsDeleted = 0 AND p.IsActive = 1
            WHERE c.Id = @CategoryId AND c.IsDeleted = 0
            GROUP BY c.Id, c.Name, c.IsActive";

        var document = await connection.QueryFirstOrDefaultAsync<CategorySearchDocument>(sql, new { CategoryId = categoryId });

        if (document == null)
        {
            _logger.LogWarning("Category {CategoryId} not found for sync", categoryId);
            return;
        }

        await _searchService.IndexCategoryAsync(document, ct);
        _logger.LogInformation("Category {CategoryId} synced to Elasticsearch", categoryId);
    }

    public async Task SyncAllCategoriesAsync(CancellationToken ct = default)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                c.Id        AS CategoryId,
                c.Name      AS Name,
                c.IsActive  AS IsActive,
                COUNT(DISTINCT p.Id) AS ProductCount
            FROM Categories c
            LEFT JOIN Brands b ON b.CategoryId = c.Id AND b.IsDeleted = 0
            LEFT JOIN Products p ON p.BrandId = b.Id AND p.IsDeleted = 0 AND p.IsActive = 1
            WHERE c.IsActive = 1 AND c.IsDeleted = 0
            GROUP BY c.Id, c.Name, c.IsActive";

        var documents = (await connection.QueryAsync<CategorySearchDocument>(sql)).ToList();
        await _bulkService.BulkIndexCategoriesAsync(documents, ct);

        _logger.LogInformation("Synced {Count} categories to Elasticsearch", documents.Count);
    }

    public async Task SyncBrandAsync(int brandId, CancellationToken ct = default)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                b.Id        AS BrandId,
                b.Name      AS Name,
                b.CategoryId AS CategoryId,
                c.Name      AS CategoryName,
                b.IsActive  AS IsActive,
                COUNT(DISTINCT p.Id) AS ProductCount
            FROM Brands b
            INNER JOIN Categories c ON b.CategoryId = c.Id
            LEFT JOIN Products p ON p.BrandId = b.Id AND p.IsDeleted = 0 AND p.IsActive = 1
            WHERE b.Id = @BrandId AND b.IsDeleted = 0
            GROUP BY b.Id, b.Name, b.CategoryId, c.Name, b.IsActive";

        var document = await connection.QueryFirstOrDefaultAsync<BrandSearchDocument>(sql, new { BrandId = brandId });

        if (document == null)
        {
            _logger.LogWarning("Brand {BrandId} not found for sync", brandId);
            return;
        }

        await _searchService.IndexBrandAsync(document, ct);
        _logger.LogInformation("Brand {BrandId} synced to Elasticsearch", brandId);
    }

    public async Task SyncAllBrandsAsync(CancellationToken ct = default)
    {
        using var connection = _sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                b.Id        AS BrandId,
                b.Name      AS Name,
                b.CategoryId AS CategoryId,
                c.Name      AS CategoryName,
                b.IsActive  AS IsActive,
                COUNT(DISTINCT p.Id) AS ProductCount
            FROM Brands b
            INNER JOIN Categories c ON b.CategoryId = c.Id
            LEFT JOIN Products p ON p.BrandId = b.Id AND p.IsDeleted = 0 AND p.IsActive = 1
            WHERE b.IsActive = 1 AND b.IsDeleted = 0
            GROUP BY b.Id, b.Name, b.CategoryId, c.Name, b.IsActive";

        var documents = (await connection.QueryAsync<BrandSearchDocument>(sql)).ToList();
        await _bulkService.BulkIndexBrandsAsync(documents, ct);

        _logger.LogInformation("Synced {Count} brands to Elasticsearch", documents.Count);
    }

    public async Task FullSyncAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting full sync from database to Elasticsearch");

        await SyncAllCategoriesAsync(ct);
        await SyncAllBrandsAsync(ct);
        await SyncAllProductsAsync(ct);

        _logger.LogInformation("Full sync completed");
    }
}