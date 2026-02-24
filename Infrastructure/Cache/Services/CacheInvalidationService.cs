namespace Infrastructure.Cache.Services;

/// <summary>
/// پیاده‌سازی سرویس Invalidation کش با Redis.
/// از Pattern-based deletion برای پاک کردن هدفمند استفاده می‌کند.
/// </summary>
public sealed class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheService cache,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task InvalidateProductAsync(int productId, CancellationToken ct = default)
    {
        var keysToInvalidate = new[]
        {
            CacheKeys.Product(productId),
            CacheKeys.CategoryProducts(productId), 
        };

        foreach (var key in keysToInvalidate)
            await _cache.ClearAsync(key);

        
        await _cache.ClearByPrefixAsync($"product:{productId}:");

        _logger.LogDebug("[CacheInvalidation] Product {ProductId} invalidated.", productId);
    }

    public async Task InvalidateCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await _cache.ClearAsync(CacheKeys.Category(categoryId));
        await _cache.ClearAsync(CacheKeys.CategoryTree());
        await _cache.ClearByPrefixAsync($"category:{categoryId}:");

        _logger.LogDebug("[CacheInvalidation] Category {CategoryId} invalidated.", categoryId);
    }

    public async Task InvalidateInventoryAsync(int variantId, CancellationToken ct = default)
    {
        await _cache.ClearAsync(CacheKeys.Inventory(variantId));
        await _cache.ClearAsync(CacheKeys.InventoryStatus(variantId));

        _logger.LogDebug("[CacheInvalidation] Inventory for Variant {VariantId} invalidated.", variantId);
    }

    public async Task InvalidateCartAsync(string cartKey, CancellationToken ct = default)
    {
        await _cache.ClearAsync(CacheKeys.Cart(cartKey));

        _logger.LogDebug("[CacheInvalidation] Cart {CartKey} invalidated.", cartKey);
    }

    public async Task InvalidateUserOrdersAsync(int userId, CancellationToken ct = default)
    {
        await _cache.ClearAsync(CacheKeys.UserOrders(userId));
        await _cache.ClearByPrefixAsync($"orders:user:{userId}:");

        _logger.LogDebug("[CacheInvalidation] User {UserId} orders cache invalidated.", userId);
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default)
    {
        await _cache.ClearByPrefixAsync(pattern);
        _logger.LogDebug("[CacheInvalidation] Pattern '{Pattern}' invalidated.", pattern);
    }
}