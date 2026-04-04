using Application.Category.Features.Shared;
using SharedKernel.Models;

namespace Application.Category.Contracts;

public interface ICategoryQueryService
{
    Task<PaginatedResult<CategoryListItemDto>> GetPagedAsync(
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken ct = default);

    Task<CategoryWithBrandsDto?> GetCategoryWithBrandsAsync(
        int id,
        CancellationToken ct = default);
}