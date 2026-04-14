using Application.Cache.Contracts;
using Application.Cache.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Cart.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Cache.Services;

public sealed class CacheInvalidationService(ICacheService cache) : ICacheInvalidationService
{
    public async Task InvalidateProductCacheAsync(ProductId productId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Product(productId.Value), ct);
        await cache.RemoveByPrefixAsync("products:", ct);
    }

    public async Task InvalidateCategoryCacheAsync(CategoryId categoryId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Category(categoryId.Value), ct);
        await cache.RemoveAsync(CacheKeys.CategoryTree(), ct);
        await cache.RemoveAsync(CacheKeys.CategoryList(), ct);
    }

    public async Task InvalidateBrandCacheAsync(BrandId brandId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Brand(brandId.Value), ct);
        await cache.RemoveAsync(CacheKeys.BrandList(), ct);
    }

    public async Task InvalidateUserCacheAsync(UserId userId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.UserProfile(userId.Value), ct);
    }

    public async Task InvalidateCartCacheAsync(CartId cartId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Cart(cartId.Value), ct);
    }

    public async Task InvalidateInventoryCacheAsync(VariantId variantId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Inventory(variantId.Value), ct);
    }
}