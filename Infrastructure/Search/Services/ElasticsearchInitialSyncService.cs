using Application.Audit.Contracts;
using Application.Search.Contracts;
using Application.Search.Features.Shared;
using Dapper;

namespace Infrastructure.Search.Services;

public sealed class ElasticsearchInitialSyncService(
    ISqlConnectionFactory sqlConnectionFactory,
    IElasticBulkService bulkService,
    IAuditService auditService)
{
    public async Task SyncAllDataAsync(CancellationToken ct = default)
    {
        await auditService.LogInformationAsync("Starting initial sync from database to Elasticsearch", ct);

        await SyncCategoriesAsync(ct);
        await SyncBrandsAsync(ct);
        await SyncProductsAsync(ct);

        await auditService.LogInformationAsync("Initial sync completed successfully", ct);
    }

    private async Task SyncCategoriesAsync(CancellationToken ct)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

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
            await auditService.LogWarningAsync("No categories found in database", ct);
            return;
        }

        await bulkService.BulkIndexCategoriesAsync(documents, ct);
        await auditService.LogInformationAsync($"Synced {documents.Count} categories", ct);
    }

    private async Task SyncBrandsAsync(CancellationToken ct)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

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
            await auditService.LogWarningAsync("No brands found in database", ct);
            return;
        }

        await bulkService.BulkIndexBrandsAsync(documents, ct);
        await auditService.LogInformationAsync($"Synced {documents.Count} brands", ct);
    }

    private async Task SyncProductsAsync(CancellationToken ct)
    {
        const int batchSize = 1000;
        var offset = 0;
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
                MIN(pv.SellingPrice) AS Price,
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
            GROUP BY p.Id, p.Name, p.Description, p.Slug, b.Id, b.Name, b.Slug, c.Id, c.Name,
                     p.IsActive, p.CreatedAt, p.UpdatedAt
            ORDER BY p.Id
            OFFSET @Offset ROWS FETCH NEXT @BatchSize ROWS ONLY";

        while (!ct.IsCancellationRequested)
        {
            using var connection = sqlConnectionFactory.CreateConnection();

            var documents = (await connection.QueryAsync<ProductSearchDocument>(
                sql, new { Offset = offset, BatchSize = batchSize })).ToList();

            if (documents.Count == 0)
                break;

            await bulkService.BulkIndexProductsAsync(documents, ct);
            totalSynced += documents.Count;

            await auditService.LogInformationAsync(
                $"Synced products batch at offset {offset} with {documents.Count} items", ct);

            offset += batchSize;

            if (documents.Count < batchSize)
                break;

            await Task.Delay(100, ct);
        }

        await auditService.LogInformationAsync($"Total synced {totalSynced} products", ct);
    }
}