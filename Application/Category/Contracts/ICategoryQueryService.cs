namespace Application.Category.Contracts;

/// <summary>
/// سرویس کوئری دسته‌بندی‌ها - مستقیماً DTO برمی‌گرداند.
/// بدون بارگذاری Aggregate - بهینه برای خواندن.
/// </summary>
public interface ICategoryQueryService
{
    /// <summary>
    /// ساختار درختی کامل برای منو
    /// </summary>
    Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(
        CancellationToken ct = default);

    /// <summary>
    /// جزئیات یک Category به همراه گروه‌ها (Admin)
    /// </summary>
    Task<CategoryWithGroupsDto?> GetCategoryWithGroupsAsync(
        int categoryId,
        CancellationToken ct = default);

    /// <summary>
    /// لیست صفحه‌بندی شده دسته‌بندی‌ها (Admin)
    /// </summary>
    Task<PaginatedResult<CategoryListItemDto>> GetCategoriesPagedAsync(
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// محصولات یک دسته‌بندی با صفحه‌بندی
    /// </summary>
    Task<PaginatedResult<CategoryProductItemDto>> GetCategoryProductsAsync(
        int categoryId,
        bool activeOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// جزئیات یک Brand (Admin)
    /// </summary>
    Task<BrandDetailDto?> GetBrandDetailAsync(
        int groupId,
        CancellationToken ct = default);

    /// <summary>
    /// لیست صفحه‌بندی شده گروه‌ها (Admin)
    /// </summary>
    Task<PaginatedResult<BrandListItemDto>> GetBrandsPagedAsync(
        int? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);
}