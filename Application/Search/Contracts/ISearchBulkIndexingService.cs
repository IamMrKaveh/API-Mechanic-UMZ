using Application.Search.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Search.Contracts;

public interface ISearchBulkIndexingService
{
    Task<bool> BulkIndexProductsAsync(
        IEnumerable<ProductSearchDocument> products,
        CancellationToken ct = default);

    Task<bool> BulkIndexCategoriesAsync(
        IEnumerable<CategorySearchDocument> categories,
        CancellationToken ct = default);

    Task<bool> BulkIndexBrandsAsync(
        IEnumerable<BrandSearchDocument> brands,
        CancellationToken ct = default);

    Task<bool> BulkDeleteProductsAsync(
        IEnumerable<ProductId> productIds,
        CancellationToken ct = default);

    Task<bool> BulkUpdateProductsAsync(
        IEnumerable<ProductSearchDocument> products,
        CancellationToken ct = default);
}