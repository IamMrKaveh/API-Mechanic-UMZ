namespace Application.Cache.Contracts;

public interface ICacheInvalidationService
{
    Task InvalidateProductCacheAsync(Guid productId, CancellationToken ct = default);

    Task InvalidateCategoryCacheAsync(Guid categoryId, CancellationToken ct = default);

    Task InvalidateBrandCacheAsync(Guid brandId, CancellationToken ct = default);

    Task InvalidateUserCacheAsync(Guid userId, CancellationToken ct = default);

    Task InvalidateCartCacheAsync(Guid cartId, CancellationToken ct = default);

    Task InvalidateInventoryCacheAsync(Guid variantId, CancellationToken ct = default);
}