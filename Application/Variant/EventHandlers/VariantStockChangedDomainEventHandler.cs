using Application.Variant.Features.Shared;
using Domain.Variant.Events;

namespace Application.Variant.EventHandlers;

public class VariantStockChangedDomainEventHandler(
    IInventoryQueryService inventoryQueryService,
    IPublisher publisher,
    IAuditService auditService) : INotificationHandler<VariantStockChangedEvent>
{
    public async Task Handle(VariantStockChangedEvent notification, CancellationToken ct)
    {
        var variantId = notification.VariantId;
        var productId = notification.ProductId;

        var availability = await inventoryQueryService.GetVariantAvailabilityAsync(variantId, ct);

        if (availability is null)
        {
            await auditService.LogInventoryEventAsync(
                variantId,
                "VariantStockChangedEventHandlerWarning",
                $"Inventory not found for variant {variantId.Value} when handling VariantStockChangedEvent");
            return;
        }

        var appNotification = new VariantStockChangedApplicationNotification
        {
            VariantId = variantId.Value,
            ProductId = productId.Value,
            QuantityChanged = notification.QuantityChanged,
            NewOnHand = availability.StockQuantity,
            NewReserved = availability.ReservedQuantity,
            NewAvailable = availability.AvailableQuantity,
            IsInStock = availability.IsInStock
        };

        await publisher.Publish(appNotification, ct);
    }
}