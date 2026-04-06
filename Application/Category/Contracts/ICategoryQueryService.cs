using Application.Category.Features.Shared;
using SharedKernel.Models;

namespace Application.Category.Contracts;

public interface ICategoryQueryService
{
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken ct = default);

    Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetAllActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetByParentIdAsync(Guid? parentId, CancellationToken ct = default);

    Task<PaginatedResult<CategoryDto>> GetCategoriesPagedAsync(
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);
}