namespace Infrastructure.Search.Services;

/// <summary>
/// استفاده از Dapper به‌جای EF Core Include برای initial sync
/// هر batch در connection جداگانه پردازش می‌شود
/// </summary>
public class ElasticsearchInitialSyncService
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IElasticBulkService _bulkService;
    private readonly ILogger<ElasticsearchInitialSyncService> _logger;

    public ElasticsearchInitialSyncService(
        ISqlConnectionFactory sqlConnectionFactory,
        IElasticBulkService bulkService,
        ILogger<ElasticsearchInitialSyncService> logger
        )
    {
        _sqlConnectionFactory = sqlConnectionFactory;
        _bulkService = bulkService;
        _logger = logger;
    }

    public async Task SyncAllDataAsync(
        CancellationToken ct = default
        )
    {
        _logger.LogInformation("Starting initial sync from database to Elasticsearch");

        await SyncCategoriesAsync(ct);
        await SyncBrandsAsync(ct);
        await SyncProductsAsync(ct);

        _logger.LogInformation("Initial sync completed successfully");
    }

    private async Task SyncCategoriesAsync(
        CancellationToken ct
        )
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

        if (documents.Count == 0)
        {
            _logger.LogWarning("No categories found in database");
            return;
        }

        await _bulkService.BulkIndexCategoriesAsync(documents, ct);
        _logger.LogInformation("Synced {Count} categories", documents.Count);
    }

    private async Task SyncBrandsAsync(CancellationToken ct)
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

        if (documents.Count == 0)
        {
            _logger.LogWarning("No brands found in database");
            return;
        }

        await _bulkService.BulkIndexBrandsAsync(documents, ct);
        _logger.LogInformation("Synced {Count} brands", documents.Count);
    }

    private async Task SyncProductsAsync(CancellationToken ct)
    {
        const int batchSize = 1000;
        var offset = 0;
        var hasMore = true;
        var totalSynced = 0;

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
            totalSynced += documents.Count;

            _logger.LogInformation("Synced products batch at offset {Offset} with {Count} items", offset, documents.Count);

            offset += batchSize;
            if (documents.Count < batchSize) hasMore = false;

            await Task.Delay(100, ct);
        }

        _logger.LogInformation("Total synced {Count} products", totalSynced);
    }
}