using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Product.Contracts;

public interface IProductQueryService
{
    Task<ProductDetailDto?> GetProductDetailAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task<AdminProductDetailDto?> GetAdminProductDetailAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task<PublicProductDetailDto?> GetPublicProductDetailAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task<PaginatedResult<ProductListItemDto>> GetAdminProductsAsync(
        Guid? categoryId,
        Guid? brandId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(
        ProductCatalogSearchParams searchParams,
        CancellationToken ct = default);
}