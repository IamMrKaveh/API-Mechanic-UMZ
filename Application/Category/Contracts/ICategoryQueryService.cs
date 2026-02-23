namespace Application.Category.Contracts;

public interface ICategoryQueryService
{
    Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(
        CancellationToken ct = default
        );

    Task<CategoryWithBrandsDto?> GetCategoryWithBrandsAsync(
        int categoryId,
        CancellationToken ct = default
        );

    Task<PaginatedResult<CategoryListItemDto>> GetCategoriesPagedAsync(
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    Task<PaginatedResult<CategoryProductItemDto>> GetCategoryProductsAsync(
        int categoryId,
        bool activeOnly,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    Task<BrandDetailDto?> GetBrandDetailAsync(
        int groupId,
        CancellationToken ct = default
        );

    Task<PaginatedResult<BrandListItemDto>> GetBrandsPagedAsync(
        int? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default
        );
}