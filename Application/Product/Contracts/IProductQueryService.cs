using Application.Product.Features.Shared;
using SharedKernel.Models;

namespace Application.Product.Contracts;

public interface IProductQueryService
{
    Task<ProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken ct = default);

    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken ct = default);

    Task<PaginatedResult<ProductListItemDto>> GetProductsPagedAsync(
        int? categoryId,
        int? brandId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductListItemDto>> GetFeaturedProductsAsync(
        int count = 10,
        CancellationToken ct = default);
}