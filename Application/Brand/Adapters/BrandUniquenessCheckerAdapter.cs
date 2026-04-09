using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Brand.Adapters;

public sealed class BrandUniquenessCheckerAdapter(IBrandRepository repository) : IBrandUniquenessChecker
{
    private readonly IBrandRepository _repository = repository;

    public bool IsUnique(BrandName name, Slug slug, BrandId? excludeId = null)
    {
        var nameExists = _repository.ExistsByNameInCategoryAsync(name, null!, excludeId).GetAwaiter().GetResult();
        var slugExists = _repository.ExistsBySlugAsync(slug, excludeId).GetAwaiter().GetResult();
        return !nameExists && !slugExists;
    }
}