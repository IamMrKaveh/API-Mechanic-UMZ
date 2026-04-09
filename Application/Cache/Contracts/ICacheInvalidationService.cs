using Domain.Brand.ValueObjects;
using Domain.Cart.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Cache.Contracts;

public interface ICacheInvalidationService
{
    Task InvalidateProductCacheAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task InvalidateCategoryCacheAsync(
        CategoryId categoryId,
        CancellationToken ct = default);

    Task InvalidateBrandCacheAsync(
        BrandId brandId,
        CancellationToken ct = default);

    Task InvalidateUserCacheAsync(
        UserId userId,
        CancellationToken ct = default);

    Task InvalidateCartCacheAsync(
        CartId cartId,
        CancellationToken ct = default);

    Task InvalidateInventoryCacheAsync(
        VariantId variantId,
        CancellationToken ct = default);
}