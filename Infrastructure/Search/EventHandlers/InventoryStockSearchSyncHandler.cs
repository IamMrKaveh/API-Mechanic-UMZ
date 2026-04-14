using Application.Audit.Contracts;
using Domain.Inventory.Events;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Search.EventHandlers;

public sealed class InventoryStockSearchSyncHandler(
    DBContext context) :
    INotificationHandler<StockIncreasedEvent>,
    INotificationHandler<StockReservedEvent>,
    INotificationHandler<StockReservationReleasedEvent>
{
    public async Task Handle(StockIncreasedEvent notification, CancellationToken ct)
        => await EnqueueProductUpdate(notification.VariantId, ct);

    public async Task Handle(StockReservedEvent notification, CancellationToken ct)
        => await EnqueueProductUpdate(notification.VariantId, ct);

    public async Task Handle(StockReservationReleasedEvent notification, CancellationToken ct)
        => await EnqueueProductUpdate(notification.VariantId, ct);

    private async Task EnqueueProductUpdate(VariantId variantId, CancellationToken ct)
    {
        var variant = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => new { v.ProductId })
            .FirstOrDefaultAsync(ct);

        if (variant is null) return;

        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            variant.ProductId.Value,
            JsonSerializer.Serialize(new { ProductId = variant.ProductId.Value }),
            "StockChanged");

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);
    }
}