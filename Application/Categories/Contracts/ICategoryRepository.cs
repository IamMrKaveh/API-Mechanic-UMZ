namespace Application.Categories.Contracts;

/// <summary>
/// Repository فقط برای Aggregate Root (Category).
/// CategoryGroup ریپازیتوری مستقل ندارد - فقط از طریق Category بارگذاری می‌شود.
/// </summary>
public interface ICategoryRepository
{
    // ==========================================
    // بازیابی Aggregate
    // ==========================================

    /// <summary>
    /// دریافت Category به همراه گروه‌ها (برای عملیات Command)
    /// </summary>
    Task<Category?> GetByIdWithGroupsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت Category به همراه گروه‌ها و محصولات (برای بررسی امکان حذف)
    /// </summary>
    Task<Category?> GetByIdWithGroupsAndProductsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت چندین Category بر اساس شناسه (برای Reorder)
    /// </summary>
    Task<IReadOnlyList<Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    /// <summary>
    /// دریافت تمام دسته‌بندی‌های فعال و غیر حذف‌شده (برای Reorder)
    /// </summary>
    Task<IReadOnlyList<Category>> GetAllActiveAsync(CancellationToken ct = default);

    // ==========================================
    // اعتبارسنجی یکتایی
    // ==========================================

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null, CancellationToken ct = default);

    // ==========================================
    // ذخیره‌سازی
    // ==========================================

    Task AddAsync(Category category, CancellationToken ct = default);

    void Update(Category category);

    void SetOriginalRowVersion(Category entity, byte[] rowVersion);
}