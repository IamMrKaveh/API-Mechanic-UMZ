using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Domain.Brand.Interfaces;

public interface IBrandUniquenessChecker
{
    bool IsUnique(
        BrandName name,
        Slug slug,
        CategoryId categoryId,
        BrandId? excludeId = null);
}