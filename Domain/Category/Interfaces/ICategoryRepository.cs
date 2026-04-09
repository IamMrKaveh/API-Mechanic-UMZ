using Domain.Category.ValueObjects;

namespace Domain.Category.Interfaces;

public interface ICategoryRepository
{
    Task<Aggregates.Category?> GetByIdAsync(
        CategoryId id,
        CancellationToken ct = default);

    Task<Aggregates.Category?> GetBySlugAsync(
        Slug slug,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Category>> GetByParentIdAsync(
        CategoryId? parentId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Category>> GetAllActiveAsync(
        CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(
        CategoryName name,
        CategoryId? excludeId = null,
        CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(
        Slug slug,
        CategoryId? excludeId = null,
        CancellationToken ct = default);

    Task<bool> HasChildrenAsync(
        CategoryId id,
        CancellationToken ct = default);

    Task AddAsync(
        Aggregates.Category category,
        CancellationToken ct = default);

    void Update(Aggregates.Category category);

    void SetOriginalRowVersion(
        Aggregates.Category entity,
        byte[] rowVersion);
}