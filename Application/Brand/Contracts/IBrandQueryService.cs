using Application.Brand.Features.Shared;
using SharedKernel.Models;

namespace Application.Brand.Contracts;

public interface IBrandQueryService
{
    Task<BrandDetailDto?> GetBrandDetailAsync(Guid brandId, CancellationToken ct = default);

    Task<BrandDto?> GetBrandBySlugAsync(string slug, CancellationToken ct = default);

    Task<PaginatedResult<BrandListItemDto>> GetBrandsPagedAsync(
        Guid? categoryId,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<BrandListItemDto>> GetPublicBrandsAsync(
        Guid? categoryId = null,
        CancellationToken ct = default);
}