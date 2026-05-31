using Application.Cache.Features.Shared;
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

    public async Task InvalidateUserCacheAsync(UserId userId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.UserProfile(userId.Value), ct);
    }

    public async Task InvalidateInventoryCacheAsync(VariantId variantId, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Inventory(variantId.Value), ct);
    }
}