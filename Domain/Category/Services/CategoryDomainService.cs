namespace Domain.Category.Services;

public class CategoryDomainService
{
    public static void MoveGroup(Aggregates.Category sourceCategory, Aggregates.Category targetCategory, int groupId)
    {
        Guard.Against.Null(sourceCategory, nameof(sourceCategory));
        Guard.Against.Null(targetCategory, nameof(targetCategory));

        if (sourceCategory.Id == targetCategory.Id)
            throw new DomainException("گروه در حال حاضر در این دسته‌بندی قرار دارد.");

        if (!targetCategory.IsActive)
            throw new DomainException("امکان انتقال به دسته‌بندی غیرفعال وجود ندارد.");

        var oldCategoryId = sourceCategory.Id;

        var group = sourceCategory.DetachBrand(groupId);

        targetCategory.AcceptBrand(group);

        sourceCategory.AddDomainEvent(
            new BrandMovedEvent(groupId, oldCategoryId, targetCategory.Id));
    }

    public static Result ValidateCategoryMerge(Aggregates.Category sourceCategory, Aggregates.Category targetCategory)
    {
        Guard.Against.Null(sourceCategory, nameof(sourceCategory));
        Guard.Against.Null(targetCategory, nameof(targetCategory));

        if (sourceCategory.Id == targetCategory.Id)
            return Result.Failure("امکان ادغام دسته‌بندی با خودش وجود ندارد.");

        if (!sourceCategory.IsActive)
            return Result.Failure("دسته‌بندی مبدأ غیرفعال است.");

        if (!targetCategory.IsActive)
            return Result.Failure("دسته‌بندی مقصد غیرفعال است.");

        foreach (var sourceGroup in sourceCategory.Brands)
        {
            if (targetCategory.ContainsGroupWithName(sourceGroup.Name))
                return Result.Failure($"گروه '{sourceGroup.Name}' در دسته‌بندی مقصد وجود دارد.");
        }

        return Result.Success();
    }

    public static void ReorderCategories(IReadOnlyList<Aggregates.Category> categories, IReadOnlyList<int> orderedIds)
    {
        var categoryDict = categories
            .Where(c => c.IsActive)
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
}