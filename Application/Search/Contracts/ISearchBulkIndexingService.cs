using Application.Search.Features.Shared;

namespace Application.Search.Contracts;

public interface ISearchBulkIndexingService
{
    Task<bool> BulkIndexProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default);

    Task<bool> BulkIndexCategoriesAsync(IEnumerable<CategorySearchDocument> categories, CancellationToken ct = default);

    Task<bool> BulkIndexBrandsAsync(IEnumerable<BrandSearchDocument> brands, CancellationToken ct = default);

    Task<bool> BulkDeleteProductsAsync(IEnumerable<int> productIds, CancellationToken ct = default);

    Task<bool> BulkUpdateProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default);
}