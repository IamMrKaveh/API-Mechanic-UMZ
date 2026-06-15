using Domain.Category.ValueObjects;

namespace Domain.Category.Interfaces;

public interface ICategoryUniquenessChecker
{
    Task<bool> IsUniqueAsync(
        CategoryName name,
        CategorySlug slug,
        CategoryId? excludeId,
        CancellationToken ct);
}