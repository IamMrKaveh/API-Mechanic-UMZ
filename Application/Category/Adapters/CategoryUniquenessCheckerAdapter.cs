using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Category.Adapters;

public sealed class CategoryUniquenessCheckerAdapter(ICategoryRepository repository)
    : ICategoryUniquenessChecker
{
    private readonly ICategoryRepository _repository = repository;

    public bool IsUnique(CategoryName name, Slug slug, CategoryId? excludeId = null)
    {
        var nameExists = _repository.ExistsByNameAsync(name, excludeId).GetAwaiter().GetResult();
        var slugExists = _repository.ExistsBySlugAsync(slug, excludeId).GetAwaiter().GetResult();
        return !nameExists && !slugExists;
    }
}