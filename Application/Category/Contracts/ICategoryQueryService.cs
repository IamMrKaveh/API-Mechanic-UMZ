using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Category.Contracts;

public interface ICategoryQueryService
{
    Task<CategoryDto?> GetCategoryByIdAsync(
        CategoryId categoryId,
        CancellationToken ct = default);

    Task<CategoryDto?> GetCategoryBySlugAsync(
        Slug slug,
        CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetAllActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken ct = default);

    Task<PaginatedResult<CategoryDto>> GetCategoriesPagedAsync(
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);
}