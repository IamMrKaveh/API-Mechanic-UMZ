using Application.Search.Contracts;
using Application.Search.Features.Shared;
using Domain.Product.ValueObjects;

namespace Infrastructure.Search.Services;

public sealed class NoOpElasticBulkService(IAuditService auditService) : IElasticBulkService
{
    public Task<bool> BulkIndexProductsAsync(
        IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> BulkIndexCategoriesAsync(
        IEnumerable<CategorySearchDocument> categories, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> BulkIndexBrandsAsync(
        IEnumerable<BrandSearchDocument> brands, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> BulkDeleteProductsAsync(
        IEnumerable<ProductId> productIds, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> BulkUpdateProductsAsync(
        IEnumerable<ProductSearchDocument> products, CancellationToken ct = default)
        => Task.FromResult(true);
}