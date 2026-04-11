using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Interfaces;

public interface IBrandUniquenessChecker
{
    bool IsUnique(
        BrandName name,
        Slug slug,
        CategoryId categoryId,
        BrandId? excludeId = null);
}