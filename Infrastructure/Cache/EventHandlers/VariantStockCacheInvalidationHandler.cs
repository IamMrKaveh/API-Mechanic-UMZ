namespace Infrastructure.Cache.EventHandlers;

/// <summary>
/// FIX #7: Invalidate و بروزرسانی Cache موجودی واریانت پس از تغییر stock
/// بدون نیاز به رفت DB - از payload کامل رویداد استفاده می‌کند (FIX #10)
/// </summary>
public class VariantStockCacheInvalidationHandler
    : INotificationHandler<VariantStockChangedEvent>
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

    public async Task Handle(VariantStockChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate variant availability cache
            await _cacheService.ClearAsync(VariantAvailabilityCacheKey(notification.VariantId));

            // Invalidate product-level availability cache
            await _cacheService.ClearAsync(ProductAvailabilityCacheKey(notification.ProductId));

            // FIX #10: اگر رویداد اطلاعات کامل داشته باشد، Cache را Populate کن (بدون DB round-trip)
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
            // Cache failure نباید عملیات را fail کند
        }
    }
}

/// <summary>
/// Read Model برای Cache موجودی واریانت - FIX #7
/// </summary>
public class VariantAvailabilityCacheDto
{
    public int VariantId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public bool IsInStock { get; set; }
    public bool IsUnlimited { get; set; }
    public DateTime LastUpdated { get; set; }
}