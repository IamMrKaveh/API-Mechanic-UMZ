using Infrastructure.Cache.Options;

namespace Infrastructure.Cache.Services;

public sealed class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly CacheOptions _cacheOptions;

    public CacheInvalidationService(
        ICacheService cache,
        ILogger<CacheInvalidationService> logger,
        IOptions<CacheOptions> cacheOptions)
    {
        _cache = cache;
        _logger = logger;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task InvalidateProductAsync(int productId, CancellationToken ct = default)
    {
        if (!_cacheOptions.IsEnabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping product cache invalidation for Product {ProductId}", productId);
            return;
        }

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
        if (!_cacheOptions.IsEnabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping category cache invalidation for Category {CategoryId}", categoryId);
            return;
        }

        await _cache.ClearAsync(CacheKeys.Category(categoryId));
        await _cache.ClearAsync(CacheKeys.CategoryTree());
        await _cache.ClearByPrefixAsync($"category:{categoryId}:");
        _logger.LogDebug("[CacheInvalidation] Category {CategoryId} invalidated.", categoryId);
    }

    public async Task InvalidateInventoryAsync(int variantId, CancellationToken ct = default)
    {
        if (!_cacheOptions.IsEnabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping inventory cache invalidation for Variant {VariantId}", variantId);
            return;
        }

        await _cache.ClearAsync(CacheKeys.Inventory(variantId));
        await _cache.ClearAsync(CacheKeys.InventoryStatus(variantId));
        _logger.LogDebug("[CacheInvalidation] Inventory for Variant {VariantId} invalidated.", variantId);
    }

    public async Task InvalidateCartAsync(string cartKey, CancellationToken ct = default)
    {
        if (!_cacheOptions.IsEnabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping cart cache invalidation for Cart {CartKey}", cartKey);
            return;
        }

        await _cache.ClearAsync(CacheKeys.Cart(cartKey));
        _logger.LogDebug("[CacheInvalidation] Cart {CartKey} invalidated.", cartKey);
    }

    public async Task InvalidateUserOrdersAsync(int userId, CancellationToken ct = default)
    {
        if (!_cacheOptions.IsEnabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping user orders cache invalidation for User {UserId}", userId);
            return;
        }

        await _cache.ClearAsync(CacheKeys.UserOrders(userId));
        await _cache.ClearByPrefixAsync($"orders:user:{userId}:");
        _logger.LogDebug("[CacheInvalidation] User {UserId} orders cache invalidated.", userId);
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default)
    {
        if (!_cacheOptions.IsEnabled)
        {
            _logger.LogDebug("Cache is disabled. Skipping pattern cache invalidation for Pattern '{Pattern}'", pattern);
            return;
        }

        await _cache.ClearByPrefixAsync(pattern);
        _logger.LogDebug("[CacheInvalidation] Pattern '{Pattern}' invalidated.", pattern);
    }
}