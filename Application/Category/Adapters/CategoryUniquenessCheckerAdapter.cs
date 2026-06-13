using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Application.Category.Adapters;

public sealed class CategoryUniquenessCheckerAdapter(ICategoryRepository categoryRepository) : ICategoryUniquenessChecker
{
    public async Task<bool> IsUniqueAsync(
        CategoryName name,
        Slug slug,
        CategoryId? excludeId,
        CancellationToken ct)
    {
        var nameExists = await categoryRepository.ExistsByNameAsync(name, excludeId, ct);
        if (nameExists)
            return false;

        var slugExists = await categoryRepository.ExistsBySlugAsync(slug, excludeId, ct);
        return !slugExists;
    }
}