namespace Application.Categories.Contracts;

public interface ICategoryGroupRepository
{
    /// <summary>
    /// دریافت گروه به همراه دسته‌بندی
    /// </summary>
    Task<CategoryGroup?> GetWithCategoryAsync(int id);

    /// <summary>
    /// دریافت گروه به همراه محصولات
    /// </summary>
    Task<CategoryGroup?> GetWithProductsAsync(int id);

    /// <summary>
    /// دریافت لیست صفحه‌بندی شده
    /// </summary>
    Task<(IEnumerable<CategoryGroup> Items, int TotalCount)> GetPagedAsync(
        int? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize);

    /// <summary>
    /// دریافت گروه‌های یک دسته‌بندی
    /// </summary>
    Task<IEnumerable<CategoryGroup>> GetByCategoryIdAsync(int categoryId);

    /// <summary>
    /// دریافت گروه‌های فعال یک دسته‌بندی
    /// </summary>
    Task<IEnumerable<CategoryGroup>> GetActiveByCategoryIdAsync(int categoryId);

    /// <summary>
    /// بررسی وجود نام تکراری در دسته‌بندی
    /// </summary>
    Task<bool> ExistsByNameInCategoryAsync(string name, int categoryId, int? excludeId = null);

    /// <summary>
    /// بررسی وجود Slug تکراری
    /// </summary>
    Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null);

    /// <summary>
    /// دریافت محصولات گروه
    /// </summary>
    Task<(IEnumerable<Domain.Product.Product> Items, int TotalCount)> GetProductsAsync(
        int groupId,
        bool activeOnly,
        int page,
        int pageSize);

    /// <summary>
    /// شمارش محصولات فعال گروه
    /// </summary>
    Task<int> CountActiveProductsAsync(int groupId);

    /// <summary>
    /// تنظیم RowVersion اصلی برای کنترل همزمانی
    /// </summary>
    void SetOriginalRowVersion(CategoryGroup entity, byte[] rowVersion);
}