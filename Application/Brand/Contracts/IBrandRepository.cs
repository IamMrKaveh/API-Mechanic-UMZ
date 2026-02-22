namespace Application.Brand.Contracts;

public interface IBrandRepository
{
    /// <summary>
    /// دریافت گروه به همراه دسته‌بندی
    /// </summary>
    Task<Domain.Brand.Brand?> GetWithCategoryAsync(
        int id
        );

    /// <summary>
    /// دریافت گروه به همراه محصولات
    /// </summary>
    Task<Domain.Brand.Brand?> GetWithProductsAsync(
        int id
        );

    /// <summary>
    /// دریافت لیست صفحه‌بندی شده
    /// </summary>
    Task<(IEnumerable<Domain.Brand.Brand> Items, int TotalCount)> GetPagedAsync(
        int? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize
        );

    /// <summary>
    /// دریافت گروه‌های یک دسته‌بندی
    /// </summary>
    Task<IEnumerable<Domain.Brand.Brand>> GetByCategoryIdAsync(
        int categoryId
        );

    /// <summary>
    /// دریافت گروه‌های فعال یک دسته‌بندی
    /// </summary>
    Task<IEnumerable<Domain.Brand.Brand>> GetActiveByCategoryIdAsync(
        int categoryId
        );

    /// <summary>
    /// بررسی وجود نام تکراری در دسته‌بندی
    /// </summary>
    Task<bool> ExistsByNameInCategoryAsync(
        string name,
        int categoryId,
        int? excludeId = null
        );

    /// <summary>
    /// بررسی وجود Slug تکراری
    /// </summary>
    Task<bool> ExistsBySlugAsync(
        string slug,
        int? excludeId = null
        );

    /// <summary>
    /// دریافت محصولات گروه
    /// </summary>
    Task<(IEnumerable<Domain.Product.Product> Items, int TotalCount)> GetProductsAsync(
        int brandId,
        bool activeOnly,
        int page,
        int pageSize
        );

    /// <summary>
    /// شمارش محصولات فعال گروه
    /// </summary>
    Task<int> CountActiveProductsAsync(
        int brandId
        );

    /// <summary>
    /// تنظیم RowVersion اصلی برای کنترل همزمانی
    /// </summary>
    void SetOriginalRowVersion(
        Domain.Brand.Brand entity,
        byte[] rowVersion
        );
}