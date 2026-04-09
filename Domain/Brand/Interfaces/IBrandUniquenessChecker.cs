using Domain.Brand.ValueObjects;

namespace Domain.Brand.Interfaces;

public interface IBrandUniquenessChecker
{
    bool IsUnique(
        BrandName name,
        Slug slug,
        BrandId? excludeId = null);
}