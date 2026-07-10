using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Application.Brand.Adapters;

public sealed class BrandUniquenessCheckerAdapter(IBrandRepository brandRepository) : IBrandUniquenessChecker
{
    public async Task<bool> IsUniqueAsync(
        BrandName name,
        BrandSlug slug,
        CategoryId categoryId,
        BrandId? excludeId,
        CancellationToken ct)
    {
        var nameExists = await brandRepository.ExistsByNameInCategoryAsync(name, categoryId, excludeId, ct);
        if (nameExists)
            return false;

        var slugExists = await brandRepository.ExistsBySlugAsync(slug, excludeId, ct);
        return !slugExists;
    }
}