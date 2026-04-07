using Domain.Category.ValueObjects;

namespace Domain.Category.Interfaces;

public interface ICategoryUniquenessChecker
{
    bool IsUnique(string name, string slug, CategoryId? excludeId = null);
}