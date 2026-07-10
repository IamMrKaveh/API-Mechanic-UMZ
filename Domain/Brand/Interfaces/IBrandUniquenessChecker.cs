using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Interfaces;

public interface IBrandUniquenessChecker
{
    Task<bool> IsUniqueAsync(
        BrandName name,
        BrandSlug slug,
        CategoryId categoryId,
        BrandId? excludeId,
        CancellationToken ct);
}