using Domain.Category.ValueObjects;

namespace Domain.Category.Interfaces;

public interface ICategoryUniquenessChecker
{
    bool IsUnique(
        CategoryName name,
        Slug slug,
        CategoryId? excludeId = null);
}