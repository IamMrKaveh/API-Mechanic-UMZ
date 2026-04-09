using Application.Product.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Common.ValueObjects;
using Domain.Product.ValueObjects;

namespace Application.Product.Contracts;

public interface IProductQueryService
{
    Task<ProductDetailDto?> GetProductDetailAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task<ProductDetailDto?> GetProductBySlugAsync(
        Slug slug,
        CancellationToken ct = default);

    Task<PaginatedResult<ProductListItemDto>> GetProductsPagedAsync(
        CategoryId? categoryId,
        BrandId? brandId,
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