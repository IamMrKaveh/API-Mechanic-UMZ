using Application.Cache.Contracts;
using Application.Cache.Features.Shared;
using Application.Audit.Contracts;
using Application.Variant.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Cache.EventHandlers;

public sealed class VariantStockCacheInvalidationHandler(
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
            var variantId = VariantId.From(notification.VariantId);
            var productId = ProductId.From(notification.ProductId);

            await cacheService.ClearAsync(VariantAvailabilityCacheKey(variantId), ct);
            await cacheService.ClearAsync(ProductAvailabilityCacheKey(productId), ct);

            if (notification.NewAvailable >= 0)
            {
                var cacheDto = new VariantAvailabilityCache(
                    notification.VariantId,
                    notification.NewAvailable,
                    false,
                    notification.IsInStock,
                    notification.NewAvailable <= 5 && notification.NewAvailable > 0,
                    DateTime.UtcNow);

                await cacheService.SetAsync(
                    VariantAvailabilityCacheKey(variantId),
                    cacheDto,
                    TimeSpan.FromMinutes(2),
                    ct);
            }

            await auditService.LogDebugAsync(
                $"Cache invalidated for Variant {notification.VariantId} (Product {notification.ProductId})", ct);
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync(
                $"Failed to invalidate cache for Variant {notification.VariantId}", ct);
        }
    }
}