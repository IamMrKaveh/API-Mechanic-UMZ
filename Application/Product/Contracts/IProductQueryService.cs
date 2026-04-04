using Application.Product.Features.Shared;
using SharedKernel.Models;

namespace Application.Product.Contracts;

public interface IProductQueryService
{
    Task<ProductDto?> GetByIdAsync(
        int id,
        CancellationToken ct = default);

    Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken ct = default);

    Task<AdminProductDetailDto?> GetAdminProductDetailAsync(
        int productId,
        CancellationToken ct = default);

    Task<PaginatedResult<AdminProductListItemDto>> GetAdminProductsAsync(
        AdminProductSearchParams searchParams,
        CancellationToken ct = default);

    Task<PublicProductDetailDto?> GetPublicProductDetailAsync(
        int productId,
        CancellationToken ct = default);

    Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(
        ProductCatalogSearchParams searchParams,
        CancellationToken ct = default);
}