using Application.Audit.Contracts;
using Application.Search.Contracts;
using Application.Search.Features.Shared;
using Domain.Product.ValueObjects;
using Elastic.Clients.Elasticsearch;
using System.Text.Json;

namespace Infrastructure.Search.Services;

public sealed class ElasticBulkService(
    ElasticsearchClient client,
    IAuditService auditService,
    ElasticsearchMetrics metrics) : IElasticBulkService
{
    public async Task<bool> BulkIndexProductsAsync(
        IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
    {
        var productList = products.ToList();
        if (!productList.Any()) return true;

        try
        {
            var response = await client.BulkAsync(b => b
                .Index("products_v1")
                .IndexMany(productList, (op, doc) => op.Index("products_v1").Id(doc.ProductId)), ct);

            if (!response.IsValidResponse)
            {
                await auditService.LogErrorAsync(
                    $"Bulk index products failed: {response.DebugInformation}", ct);
                return false;
            }

            metrics.RecordBulkOperationSuccess(productList.Count, "products_v1");
            await auditService.LogInformationAsync(
                $"Bulk indexed {productList.Count} products", ct);
            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Exception during bulk index products: {ex.Message}", ct);
            throw;
        }
    }

    public async Task<bool> BulkIndexCategoriesAsync(
        IEnumerable<CategorySearchDocument> categories, CancellationToken ct = default)
    {
        var list = categories.ToList();
        if (!list.Any()) return true;

        try
        {
            var response = await client.BulkAsync(b => b
                .Index("categories_v1")
                .IndexMany(list, (op, doc) => op.Index("categories_v1").Id(doc.CategoryId)), ct);

            if (!response.IsValidResponse)
            {
                await auditService.LogErrorAsync(
                    $"Bulk index categories failed: {response.DebugInformation}", ct);
                return false;
            }

            metrics.RecordBulkOperationSuccess(list.Count, "categories_v1");
            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Exception during bulk index categories: {ex.Message}", ct);
            throw;
        }
    }

    public async Task<bool> BulkIndexBrandsAsync(
        IEnumerable<BrandSearchDocument> brands, CancellationToken ct = default)
    {
        var list = brands.ToList();
        if (!list.Any()) return true;

        try
        {
            var response = await client.BulkAsync(b => b
                .Index("brands_v1")
                .IndexMany(list, (op, doc) => op.Index("brands_v1").Id(doc.BrandId)), ct);

            if (!response.IsValidResponse)
            {
                await auditService.LogErrorAsync(
                    $"Bulk index brands failed: {response.DebugInformation}", ct);
                return false;
            }

            metrics.RecordBulkOperationSuccess(list.Count, "brands_v1");
            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Exception during bulk index brands: {ex.Message}", ct);
            throw;
        }
    }

    public async Task<bool> BulkDeleteProductsAsync(
        IEnumerable<ProductId> productIds, CancellationToken ct = default)
    {
        var idList = productIds.ToList();
        if (!idList.Any()) return true;

        try
        {
            var response = await client.BulkAsync(b => b
                .Index("products_v1")
                .DeleteMany(idList, (op, id) => op.Index("products_v1").Id(id.Value)), ct);

            if (!response.IsValidResponse)
            {
                await auditService.LogErrorAsync(
                    $"Bulk delete products failed: {response.DebugInformation}", ct);
                return false;
            }

            metrics.RecordBulkOperationSuccess(idList.Count, "products_v1");
            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Exception during bulk delete products: {ex.Message}", ct);
            throw;
        }
    }

    public async Task<bool> BulkUpdateProductsAsync(
        IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
    {
        var productList = products.ToList();
        if (!productList.Any()) return true;

        try
        {
            var response = await client.BulkAsync(b => b
                .Index("products_v1")
                .UpdateMany(productList, (op, doc) => op
                    .Index("products_v1")
                    .Id(doc.ProductId)
                    .Doc(doc)), ct);

            if (!response.IsValidResponse)
            {
                await auditService.LogErrorAsync(
                    $"Bulk update products failed: {response.DebugInformation}", ct);
                return false;
            }

            metrics.RecordBulkOperationSuccess(productList.Count, "products_v1");
            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Exception during bulk update products: {ex.Message}", ct);
            throw;
        }
    }
}