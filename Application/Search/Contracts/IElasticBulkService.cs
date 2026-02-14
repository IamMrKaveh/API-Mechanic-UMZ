namespace Application.Search.Contracts;

public interface IElasticBulkService
{
    Task<bool> BulkIndexProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default);

    Task<bool> BulkIndexCategoriesAsync(IEnumerable<CategorySearchDocument> categories, CancellationToken ct = default);

    Task<bool> BulkIndexCategoryGroupsAsync(IEnumerable<CategoryGroupSearchDocument> categoryGroups, CancellationToken ct = default);

    Task<bool> BulkDeleteProductsAsync(IEnumerable<int> productIds, CancellationToken ct = default);

    Task<bool> BulkUpdateProductsAsync(IEnumerable<ProductSearchDocument> products, CancellationToken ct = default);
}