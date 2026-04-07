using Domain.Brand.ValueObjects;

namespace Domain.Brand.Interfaces;

public interface IBrandUniquenessChecker
{
    bool IsUnique(string name, string slug, BrandId? excludeId = null);
}