namespace Application.Category.Contracts;

/// <summary>
/// Repository فقط برای Aggregate Root (Category).
/// Brand ریپازیتوری مستقل ندارد - فقط از طریق Category بارگذاری می‌شود.
/// </summary>
public interface ICategoryRepository
{
    Task<(IEnumerable<Domain.Category.Category> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت Category به همراه گروه‌ها (برای عملیات Command)
    /// </summary>
    Task<Domain.Category.Category?> GetByIdWithGroupsAsync(
        int id,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت Category به همراه گروه‌ها و محصولات (برای بررسی امکان حذف)
    /// </summary>
    Task<Domain.Category.Category?> GetByIdWithGroupsAndProductsAsync(
        int id,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت چندین Category بر اساس شناسه (برای Reorder)
    /// </summary>
    Task<IReadOnlyList<Domain.Category.Category>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت تمام دسته‌بندی‌های فعال و غیر حذف‌شده (برای Reorder)
    /// </summary>
    Task<IReadOnlyList<Domain.Category.Category>> GetAllActiveAsync(
        CancellationToken ct = default
        );

    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeId = null,
        CancellationToken ct = default
        );

    Task<bool> ExistsBySlugAsync(
        string slug,
        int? excludeId = null,
        CancellationToken ct = default);

    Task AddAsync(
        Domain.Category.Category category,
        CancellationToken ct = default
        );

    void Update(
        Domain.Category.Category category
        );

    void SetOriginalRowVersion(
        Domain.Category.Category entity,
        byte[] rowVersion
        );
}