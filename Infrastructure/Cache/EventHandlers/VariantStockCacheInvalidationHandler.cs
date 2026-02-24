namespace Infrastructure.Cache.EventHandlers;

/// <summary>
/// Invalidate و بروزرسانی Cache موجودی واریانت پس از تغییر stock
/// بدون نیاز به رفت DB - از payload کامل رویداد استفاده می‌کند
/// </summary>
public class VariantStockCacheInvalidationHandler
    : INotificationHandler<VariantStockChangedApplicationNotification>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<VariantStockCacheInvalidationHandler> _logger;

    private static string VariantAvailabilityCacheKey(int variantId) =>
        $"inventory:availability:{variantId}";

    private static string ProductAvailabilityCacheKey(int productId) =>
        $"inventory:product-availability:{productId}";

    public VariantStockCacheInvalidationHandler(
        ICacheService cacheService,
        ILogger<VariantStockCacheInvalidationHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(VariantStockChangedApplicationNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            
            await _cacheService.ClearAsync(VariantAvailabilityCacheKey(notification.VariantId));

            
            await _cacheService.ClearAsync(ProductAvailabilityCacheKey(notification.ProductId));

            
            if (notification.NewOnHand > 0 || notification.NewAvailable >= 0)
            {
                var cacheDto = new VariantAvailabilityCacheDto
                {
                    VariantId = notification.VariantId,
                    OnHand = notification.NewOnHand,
                    Reserved = notification.NewReserved,
                    Available = notification.NewAvailable,
                    IsInStock = notification.IsInStock,
                    LastUpdated = DateTime.UtcNow
                };

                await _cacheService.SetAsync(
                    VariantAvailabilityCacheKey(notification.VariantId),
                    cacheDto,
                    TimeSpan.FromMinutes(2));
            }

            _logger.LogDebug(
                "Cache invalidated for Variant {VariantId} (Product {ProductId})",
                notification.VariantId, notification.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to invalidate cache for Variant {VariantId}",
                notification.VariantId);
        }
    }
}