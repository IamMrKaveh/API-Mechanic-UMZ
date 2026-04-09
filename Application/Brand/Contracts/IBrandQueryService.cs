using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;

namespace Application.Brand.Contracts;

public interface IBrandQueryService
{
    Task<BrandDetailDto?> GetBrandDetailAsync(
        BrandId brandId,
        CancellationToken ct = default);

    Task<BrandDto?> GetBrandBySlugAsync(
        Slug slug,
        CancellationToken ct = default);

    Task<PaginatedResult<BrandListItemDto>> GetBrandsPagedAsync(
        CategoryId? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<BrandListItemDto>> GetPublicBrandsAsync(
        CategoryId? categoryId = null,
        CancellationToken ct = default);
}