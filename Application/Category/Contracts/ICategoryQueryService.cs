using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Category.Contracts;

public interface ICategoryQueryService
{
    Task<CategoryDto?> GetCategoryByIdAsync(
        CategoryId categoryId,
        CancellationToken ct = default);

    Task<CategoryDetailDto?> GetCategoryDetailAsync(
        CategoryId categoryId,
        CancellationToken ct = default);

    Task<CategoryDto?> GetCategoryBySlugAsync(
        Slug slug,
        CancellationToken ct = default);

    Task<IReadOnlyList<CategoryDto>> GetAllActiveAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(
        CancellationToken ct = default);

    Task<PaginatedResult<CategoryListItemDto>> GetCategoriesPagedAsync(
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<CategoryWithBrandsDto?> GetCategoryWithBrandsAsync(
        CategoryId categoryId,
        CancellationToken ct = default);

    Task<PaginatedResult<CategoryProductItemDto>> GetCategoryProductsAsync(
        CategoryId categoryId,
        bool activeOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<CategoryDto>> GetPublicCategoriesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);
}