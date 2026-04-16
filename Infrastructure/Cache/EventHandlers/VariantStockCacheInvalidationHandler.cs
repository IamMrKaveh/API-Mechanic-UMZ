using Application.Cache.Features.Shared;
using Application.Variant.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Cache.EventHandlers;

/// <summary>
/// Invalidate و بروزرسانی Cache موجودی واریانت پس از تغییر stock
/// بدون نیاز به رفت DB - از payload کامل رویداد استفاده می‌کند
/// </summary>
public class VariantStockCacheInvalidationHandler(
    ICacheService cacheService,
    IAuditService auditService)
        : INotificationHandler<VariantStockChangedApplicationNotification>
{
    private static string VariantAvailabilityCacheKey(VariantId variantId) =>
        $"inventory:availability:{variantId}";

    private static string ProductAvailabilityCacheKey(ProductId productId) =>
        $"inventory:product-availability:{productId}";

    public async Task Handle(VariantStockChangedApplicationNotification notification, CancellationToken ct)
    {
        try
        {
            await cacheService.ClearAsync(VariantAvailabilityCacheKey(VariantId.From(notification.VariantId)), ct);

            await cacheService.ClearAsync(ProductAvailabilityCacheKey(ProductId.From(notification.ProductId)), ct);

            if (notification.NewOnHand > 0 || notification.NewAvailable >= 0)
            {
                var cacheDto = new VariantAvailabilityCache
                {
                    VariantId = notification.VariantId,
                    OnHand = notification.NewOnHand,
                    Reserved = notification.NewReserved,
                    Available = notification.NewAvailable,
                    IsInStock = notification.IsInStock,
                    LastUpdated = DateTime.UtcNow
                };

                await cacheService.SetAsync(
                    VariantAvailabilityCacheKey(VariantId.From(notification.VariantId)),
                    cacheDto,
                    TimeSpan.FromMinutes(2),
                    ct);
            }

            await auditService.LogDebugAsync(
                "Cache invalidated for Variant {notification.VariantId} (Product {notification.ProductId})",
                ct);
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync(
                $"Failed to invalidate cache for Variant {notification.VariantId}",
                ct);
        }
    }
}