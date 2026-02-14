namespace Domain.Categories.Services;

/// <summary>
/// Domain Service - منطق‌هایی که بین چند Aggregate هستند.
/// Stateless و بدون وابستگی به Infrastructure.
/// </summary>
public class CategoryDomainService
{
    /// <summary>
    /// انتقال گروه از یک Category به Category دیگر.
    /// بررسی عدم تداخل نام در Category مقصد انجام می‌شود.
    /// </summary>
    public void MoveGroup(Category sourceCategory, Category targetCategory, int groupId)
    {
        Guard.Against.Null(sourceCategory, nameof(sourceCategory));
        Guard.Against.Null(targetCategory, nameof(targetCategory));

        if (sourceCategory.Id == targetCategory.Id)
            throw new DomainException("گروه در حال حاضر در این دسته‌بندی قرار دارد.");

        if (targetCategory.IsDeleted)
            throw new DomainException("امکان انتقال به دسته‌بندی حذف‌شده وجود ندارد.");

        if (!targetCategory.IsActive)
            throw new DomainException("امکان انتقال به دسته‌بندی غیرفعال وجود ندارد.");

        var oldCategoryId = sourceCategory.Id;

        // جدا کردن از مبدأ
        var group = sourceCategory.DetachGroup(groupId);

        // پذیرش در مقصد (یکتایی نام در مقصد بررسی می‌شود)
        targetCategory.AcceptGroup(group);

        // رویداد روی Aggregate مبدأ ثبت می‌شود
        sourceCategory.AddDomainEvent(
            new CategoryGroupMovedEvent(groupId, oldCategoryId, targetCategory.Id));
    }

    /// <summary>
    /// اعتبارسنجی امکان ادغام دو دسته‌بندی
    /// </summary>
    public (bool CanMerge, string? Error) ValidateCategoryMerge(
        Category sourceCategory,
        Category targetCategory)
    {
        Guard.Against.Null(sourceCategory, nameof(sourceCategory));
        Guard.Against.Null(targetCategory, nameof(targetCategory));

        if (sourceCategory.Id == targetCategory.Id)
            return (false, "امکان ادغام دسته‌بندی با خودش وجود ندارد.");

        if (sourceCategory.IsDeleted)
            return (false, "دسته‌بندی مبدأ حذف شده است.");

        if (targetCategory.IsDeleted)
            return (false, "دسته‌بندی مقصد حذف شده است.");

        foreach (var sourceGroup in sourceCategory.CategoryGroups.Where(g => !g.IsDeleted))
        {
            if (targetCategory.ContainsGroupWithName(sourceGroup.Name))
            {
                return (false, $"گروه '{sourceGroup.Name}' در دسته‌بندی مقصد وجود دارد.");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// تغییر ترتیب نمایش دسته‌بندی‌ها
    /// </summary>
    public void ReorderCategories(IReadOnlyList<Category> categories, IReadOnlyList<int> orderedIds)
    {
        var categoryDict = categories
            .Where(c => !c.IsDeleted)
            .ToDictionary(c => c.Id);

        var activeCategoryIds = categoryDict.Keys.ToHashSet();
        var providedIds = orderedIds.ToHashSet();

        if (!activeCategoryIds.SetEquals(providedIds))
            throw new DomainException("لیست شناسه‌های دسته‌بندی‌ها با دسته‌بندی‌های فعال مطابقت ندارد.");

        for (int i = 0; i < orderedIds.Count; i++)
        {
            categoryDict[orderedIds[i]].SetSortOrder(i);
        }
    }

    /// <summary>
    /// محاسبه آمار دسته‌بندی
    /// </summary>
    public CategoryStatistics CalculateStatistics(Category category)
    {
        Guard.Against.Null(category, nameof(category));

        var activeGroups = category.CategoryGroups.Where(g => !g.IsDeleted && g.IsActive).ToList();
        var allGroups = category.CategoryGroups.Where(g => !g.IsDeleted).ToList();

        var totalProducts = allGroups.Sum(g => g.TotalProductsCount);
        var activeProducts = allGroups.Sum(g => g.ActiveProductsCount);

        return new CategoryStatistics(
            TotalGroups: allGroups.Count,
            ActiveGroups: activeGroups.Count,
            TotalProducts: totalProducts,
            ActiveProducts: activeProducts,
            IsEmpty: totalProducts == 0);
    }
}

public record CategoryStatistics(
    int TotalGroups,
    int ActiveGroups,
    int TotalProducts,
    int ActiveProducts,
    bool IsEmpty)
{
    public decimal ActiveGroupsPercentage =>
        TotalGroups > 0 ? Math.Round((decimal)ActiveGroups / TotalGroups * 100, 2) : 0;

    public decimal ActiveProductsPercentage =>
        TotalProducts > 0 ? Math.Round((decimal)ActiveProducts / TotalProducts * 100, 2) : 0;
}