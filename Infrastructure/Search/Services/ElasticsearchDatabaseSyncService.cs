using Application.Audit.Contracts;
using Application.Common.Contracts;
using Application.Search.Contracts;
using Application.Search.Features.Shared;
using Dapper;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Infrastructure.Search.Services;

public sealed class ElasticsearchDatabaseSyncService(
    ISqlConnectionFactory sqlConnectionFactory,
    ISearchService searchService,
    IElasticBulkService bulkService,
    IAuditService auditService) : ISearchDatabaseSyncService
{
    public async Task SyncAsync(CancellationToken ct = default)
    {
        await FullSyncAsync(ct);
        await auditService.LogInformationAsync("Sync Completed", ct);
    }

    public async Task SyncProductAsync(ProductId productId, CancellationToken ct = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

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
            LEFT JOIN ProductVariants pv ON pv.ProductId = p.Id AND pv.IsDeleted = false
            WHERE p.Id = @ProductId AND p.IsDeleted = false
            GROUP BY p.Id, p.Name, p.Description, p.Slug, b.Id, b.Name, b.Slug, c.Id, c.Name, p.IsActive, p.CreatedAt, p.UpdatedAt";

        var document = await connection.QueryFirstOrDefaultAsync<ProductSearchDocument>(
            sql, new { ProductId = productId.Value });

        if (document == null)
        {
            await auditService.LogWarningAsync(
                $"Product {productId.Value} not found for sync", ct);
            return;
        }

        await searchService.IndexProductAsync(document, ct);
        await auditService.LogInformationAsync(
            $"Product {productId.Value} synced to Elasticsearch", ct);
    }

    public async Task SyncCategoryAsync(CategoryId categoryId, CancellationToken ct = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT Id AS CategoryId, Name, Slug, IsActive
            FROM Categories
            WHERE Id = @CategoryId";

        var document = await connection.QueryFirstOrDefaultAsync<CategorySearchDocument>(
            sql, new { CategoryId = categoryId.Value });

        if (document == null) return;

        await searchService.IndexCategoryAsync(document, ct);
    }

    public async Task SyncBrandAsync(BrandId brandId, CancellationToken ct = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT b.Id AS BrandId, b.Name, b.Slug, b.IsActive,
                   c.Id AS CategoryId, c.Name AS CategoryName
            FROM Brands b
            INNER JOIN Categories c ON b.CategoryId = c.Id
            WHERE b.Id = @BrandId";

        var document = await connection.QueryFirstOrDefaultAsync<BrandSearchDocument>(
            sql, new { BrandId = brandId.Value });

        if (document == null) return;

        await searchService.IndexBrandAsync(document, ct);
    }

    public async Task SyncAllProductsAsync(CancellationToken ct = default)
    {
        const int batchSize = 500;
        var offset = 0;

        const string sql = @"
            SELECT
                p.Id AS ProductId, p.Name, p.Description, p.Slug,
                b.Id AS BrandId, b.Name AS BrandName, b.Slug AS BrandSlug,
                c.Id AS CategoryId, c.Name AS CategoryName,
                MIN(pv.SellingPrice) AS Price,
                COALESCE(SUM(pv.StockQuantity), 0) AS StockQuantity,
                p.IsActive, p.CreatedAt, p.UpdatedAt
            FROM Products p
            INNER JOIN Brands b ON p.BrandId = b.Id
            INNER JOIN Categories c ON b.CategoryId = c.Id
            LEFT JOIN ProductVariants pv ON pv.ProductId = p.Id AND pv.IsDeleted = false
            WHERE p.IsDeleted = false
            GROUP BY p.Id, p.Name, p.Description, p.Slug, b.Id, b.Name, b.Slug, c.Id, c.Name, p.IsActive, p.CreatedAt, p.UpdatedAt
            ORDER BY p.Id
            LIMIT @BatchSize OFFSET @Offset";

        while (true)
        {
            using var connection = sqlConnectionFactory.CreateConnection();
            var batch = (await connection.QueryAsync<ProductSearchDocument>(
                sql, new { BatchSize = batchSize, Offset = offset })).ToList();

            if (!batch.Any()) break;

            await bulkService.BulkIndexProductsAsync(batch, ct);
            offset += batchSize;

            await auditService.LogInformationAsync(
                $"Synced batch of {batch.Count} products (offset: {offset})", ct);
        }
    }

    public async Task SyncAllCategoriesAsync(CancellationToken ct = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = "SELECT Id AS CategoryId, Name, Slug, IsActive FROM Categories";

        var documents = (await connection.QueryAsync<CategorySearchDocument>(sql)).ToList();
        await bulkService.BulkIndexCategoriesAsync(documents, ct);
    }

    public async Task SyncAllBrandsAsync(CancellationToken ct = default)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = @"
            SELECT b.Id AS BrandId, b.Name, b.Slug, b.IsActive,
                   c.Id AS CategoryId, c.Name AS CategoryName
            FROM Brands b
            INNER JOIN Categories c ON b.CategoryId = c.Id";

        var documents = (await connection.QueryAsync<BrandSearchDocument>(sql)).ToList();
        await bulkService.BulkIndexBrandsAsync(documents, ct);
    }

    public async Task FullSyncAsync(CancellationToken ct = default)
    {
        await SyncAllCategoriesAsync(ct);
        await SyncAllBrandsAsync(ct);
        await SyncAllProductsAsync(ct);
    }
}